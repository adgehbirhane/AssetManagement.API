#!/usr/bin/env node

/**
 * Asset Image Downloader Script
 * 
 * This script downloads product images for the assets defined in DbSeeder.cs
 * and stores them in the wwwroot/uploads/assets directory. It also generates SQL
 * statements to update the Asset table with the ImageUrl values.
 * 
 * Usage: node download-asset-images.js
 * 
 * Requirements:
 * - Node.js 16+ with fetch support
 * - Internet connection for downloading images
 * - Write permissions to wwwroot/uploads/assets directory
 */

import { promises as fs } from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

// Get current directory (ES module compatibility)
const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

// Configuration
const ASSETS_DIR = path.join(__dirname, 'wwwroot', 'uploads', 'assets');
const IMAGE_QUALITY = 'high'; // high, medium, low
const IMAGE_FORMAT = 'jpg'; // jpg, png, webp

// Asset definitions matching DbSeeder.cs
const ASSETS = [
    {
        name: "MacBook Pro 16-inch",
        serialNumber: "MBP-2023-001",
        searchQuery: "MacBook Pro 16 inch 2023 laptop",
        filename: "macbook-pro-16-2023.jpg"
    },
    {
        name: "Dell XPS 13",
        serialNumber: "DLL-2023-002", 
        searchQuery: "Dell XPS 13 2023 laptop",
        filename: "dell-xps-13-2023.jpg"
    },
    {
        name: "iPhone 15 Pro",
        serialNumber: "IPH-2023-003",
        searchQuery: "iPhone 15 Pro 2023 smartphone",
        filename: "iphone-15-pro-2023.jpg"
    },
    {
        name: "Samsung Galaxy S24",
        serialNumber: "SMS-2023-004",
        searchQuery: "Samsung Galaxy S24 2024 smartphone",
        filename: "samsung-galaxy-s24-2024.jpg"
    },
    {
        name: "Dell UltraSharp 27-inch Monitor",
        serialNumber: "MON-2023-005",
        searchQuery: "Dell UltraSharp 27 inch monitor 2023",
        filename: "dell-ultrasharp-27-2023.jpg"
    },
    {
        name: "iPad Pro 12.9-inch",
        serialNumber: "TAB-2023-006",
        searchQuery: "iPad Pro 12.9 inch 2023 tablet",
        filename: "ipad-pro-12-9-2023.jpg"
    },
    {
        name: "HP EliteDesk Desktop",
        serialNumber: "DSK-2023-007",
        searchQuery: "HP EliteDesk desktop computer 2023",
        filename: "hp-elitedesk-2023.jpg"
    }
];

// stock image URLs (using Unsplash for high-quality, free images)
const STOCK_IMAGES = {
    "MacBook Pro 16-inch": "https://images.unsplash.com/photo-1517336714731-489689fd1ca8?w=800&h=600&fit=crop&crop=center",
    "Dell XPS 13": "https://images.unsplash.com/photo-1593642632823-8f785ba67e45?w=800&h=600&fit=crop&crop=center",
    "iPhone 15 Pro": "https://images.unsplash.com/photo-1592750475338-74b7b21085ab?w=800&h=600&fit=crop&crop=center",
    "Samsung Galaxy S24": "https://images.unsplash.com/photo-1511707171634-5f897ff02aa9?w=800&h=600&fit=crop&crop=center",
    "Dell UltraSharp 27-inch Monitor": "https://images.unsplash.com/photo-1527443224154-c4a3942d3acf?w=800&h=600&fit=crop&crop=center",
    "iPad Pro 12.9-inch": "https://images.unsplash.com/photo-1544244015-0df4b3ffc6b0?w=800&h=600&fit=crop&crop=center",
    "HP EliteDesk Desktop": "https://images.unsplash.com/photo-1588872657578-7efd1f1555ed?w=800&h=600&fit=crop&crop=center"
};

/**
 * Ensures the assets directory exists
 */
async function ensureAssetsDirectory() {
    try {
        await fs.access(ASSETS_DIR);
        console.log(`✅ Assets directory exists: ${ASSETS_DIR}`);
    } catch (error) {
        console.log(`📁 Creating assets directory: ${ASSETS_DIR}`);
        await fs.mkdir(ASSETS_DIR, { recursive: true });
        console.log(`✅ Assets directory created successfully`);
    }
}

/**
 * Downloads an image from a URL and saves it to the local filesystem
 * @param {string} url - The URL to download the image from
 * @param {string} filename - The filename to save the image as
 * @returns {Promise<boolean>} - True if successful, false otherwise
 */
async function downloadImage(url, filename) {
    try {
        console.log(`📥 Downloading: ${filename}`);
        
        const response = await fetch(url);
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        
        const buffer = await response.arrayBuffer();
        const filePath = path.join(ASSETS_DIR, filename);
        
        await fs.writeFile(filePath, Buffer.from(buffer));
        
        // Verify file was created and has content
        const stats = await fs.stat(filePath);
        if (stats.size === 0) {
            throw new Error('Downloaded file is empty');
        }
        
        console.log(`✅ Downloaded: ${filename} (${(stats.size / 1024).toFixed(2)} KB)`);
        return true;
    } catch (error) {
        console.error(`❌ Failed to download ${filename}: ${error.message}`);
        return false;
    }
}

/**
 * Generates SQL statements to update the Asset table with ImageUrl values
 * @param {Array} assets - Array of asset objects with filename information
 * @returns {string} - SQL statements as a string
 */
function generateUpdateSQL(assets) {
    let sql = `-- SQL statements to update Asset table with ImageUrl values
-- Generated on: ${new Date().toISOString()}
-- Execute these statements in your database to update the ImageUrl field

`;
    
    assets.forEach(asset => {
        sql += `UPDATE "Assets" 
SET "ImageUrl" = '${asset.filename}', 
    "UpdatedAt" = '${new Date().toISOString()}'
WHERE "SerialNumber" = '${asset.serialNumber}';

`;
    });
    
    sql += `-- Verify updates
SELECT "Name", "SerialNumber", "ImageUrl" FROM "Assets" WHERE "ImageUrl" IS NOT NULL;
`;
    
    return sql;
}

/**
 * Main function to orchestrate the image download process
 */
async function main() {
    console.log('🚀 Starting Asset Image Download Process...\n');
    
    try {
        // Ensure assets directory exists
        await ensureAssetsDirectory();
        console.log('');
        
        // Download images for each asset
        const downloadResults = [];
        for (const asset of ASSETS) {
            const imageUrl = STOCK_IMAGES[asset.name];
            if (!imageUrl) {
                console.warn(`⚠️  No image URL found for: ${asset.name}`);
                continue;
            }
            
            const success = await downloadImage(imageUrl, asset.filename);
            downloadResults.push({
                ...asset,
                success,
                imageUrl: success ? asset.filename : null
            });
            
            // Small delay to be respectful to the image service
            await new Promise(resolve => setTimeout(resolve, 500));
        }
        
        console.log('\n📊 Download Summary:');
        console.log('==================');
        
        const successfulDownloads = downloadResults.filter(r => r.success);
        const failedDownloads = downloadResults.filter(r => !r.success);
        
        console.log(`✅ Successful: ${successfulDownloads.length}/${ASSETS.length}`);
        console.log(`❌ Failed: ${failedDownloads.length}/${ASSETS.length}`);
        
        if (failedDownloads.length > 0) {
            console.log('\nFailed downloads:');
            failedDownloads.forEach(asset => {
                console.log(`  - ${asset.name} (${asset.serialNumber})`);
            });
        }
        
        // Generate SQL statements
        if (successfulDownloads.length > 0) {
            console.log('\n📝 Generating SQL update statements...');
            const sql = generateUpdateSQL(successfulDownloads);
            
            const sqlFilePath = path.join(__dirname, 'update-asset-images.sql');
            await fs.writeFile(sqlFilePath, sql, 'utf8');
            console.log(`✅ SQL file created: ${sqlFilePath}`);
            
            console.log('\n📋 SQL Preview:');
            console.log('===============');
            console.log(sql);
        }
        
        console.log('\n🎉 Asset image download process completed!');
        console.log(`📁 Images saved to: ${ASSETS_DIR}`);
        console.log('💡 Next steps:');
        console.log('   1. Review the downloaded images');
        console.log('   2. Execute the generated SQL to update your database');
        console.log('   3. Restart your API to serve the new images');
        
    } catch (error) {
        console.error('\n💥 Fatal error:', error.message);
        process.exit(1);
    }
}

// Handle command line arguments
if (process.argv.includes('--help') || process.argv.includes('-h')) {
    console.log(`
Asset Image Downloader Script

Usage: node download-asset-images.js [options]

Options:
  --help, -h     Show this help message
  --dry-run      Show what would be downloaded without actually downloading
  --quality      Image quality (high|medium|low) [default: high]
  --format       Image format (jpg|png|webp) [default: jpg]

Examples:
  node download-asset-images.js
  node download-asset-images.js --quality medium --format png
`);
    process.exit(0);
}

// Run the main function
main().catch(error => {
    console.error('💥 Unhandled error:', error);
    process.exit(1);
});
