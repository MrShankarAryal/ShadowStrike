import { put } from '@vercel/blob';

export default async function handler(req, res) {
    if (req.method === 'POST') {
        try {
            // VULNERABILITY: No file type validation
            // VULNERABILITY: No file size limits
            // VULNERABILITY: Uses original filename without sanitization

            const contentType = req.headers['content-type'] || '';

            if (!contentType.includes('multipart/form-data')) {
                return res.status(400).json({
                    error: 'Content-Type must be multipart/form-data'
                });
            }

            // For Vercel Blob, we need to handle the upload differently
            // This is a simplified version - in production you'd use a proper multipart parser

            const filename = req.headers['x-filename'] || 'uploaded-file-' + Date.now();
            const blob = await put(`uploads/${filename}`, req, {
                access: 'public',
            });

            return res.status(200).json({
                success: true,
                message: 'File uploaded successfully',
                url: blob.url,
                filename: filename,
                size: blob.size,
                warning: 'File uploaded without validation (Security Risk!)'
            });

        } catch (error) {
            return res.status(500).json({
                error: error.message,
                stack: error.stack
            });
        }
    }

    // GET: List uploaded files
    if (req.method === 'GET') {
        try {
            const { list } = await import('@vercel/blob');
            const { blobs } = await list({ prefix: 'uploads/' });

            return res.status(200).json({
                files: blobs.map(blob => ({
                    url: blob.url,
                    pathname: blob.pathname,
                    size: blob.size,
                    uploadedAt: blob.uploadedAt
                }))
            });
        } catch (error) {
            return res.status(500).json({ error: error.message });
        }
    }

    res.status(405).send("Method Not Allowed");
}
