import { initializeDatabase } from './db.js';

export default async function handler(req, res) {
    try {
        await initializeDatabase();

        return res.status(200).json({
            success: true,
            message: 'Database initialized successfully!',
            data: {
                users: 'Created 3 sample users (admin, user, guest)',
                articles: 'Created 2 sample articles',
                banner: 'Set default banner image'
            },
            next_steps: [
                'Visit /api/sql?id=1 to see the banner',
                'Try login with: admin / admin123',
                'Try SQL injection: admin\' OR \'1\'=\'1\' --'
            ]
        });
    } catch (error) {
        return res.status(500).json({
            success: false,
            error: error.message,
            stack: error.stack
        });
    }
}
