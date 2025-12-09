import { put, list, del } from '@vercel/blob';

// Initialize database with sample data
export async function initializeDatabase() {
    try {
        // Check if database is already initialized
        const { blobs } = await list({ prefix: 'db/users/' });
        if (blobs.length > 0) {
            console.log('Database already initialized');
            return;
        }

        // Create sample users
        const users = [
            { id: 1, username: 'admin', password: 'admin123', role: 'admin', email: 'admin@vulnerable.com' },
            { id: 2, username: 'user', password: 'password', role: 'user', email: 'user@vulnerable.com' },
            { id: 3, username: 'guest', password: 'guest', role: 'guest', email: 'guest@vulnerable.com' }
        ];

        await put('db/users/data.json', JSON.stringify(users), {
            access: 'public',
            addRandomSuffix: false
        });

        // Create sample articles
        const articles = [
            {
                id: 1,
                title: 'Welcome to Our Vulnerable Blog',
                content: 'This is a test article for security testing purposes. Feel free to test SQL injection and XSS attacks here!',
                author: 'admin',
                date: new Date().toISOString()
            },
            {
                id: 2,
                title: 'SQL Injection Tutorial',
                content: 'Learn how SQL injection works in this vulnerable application. Try bypassing the login with: admin\' OR \'1\'=\'1\' --',
                author: 'admin',
                date: new Date().toISOString()
            },
            {
                id: 3,
                title: 'Testing XSS Vulnerabilities',
                content: 'This article demonstrates stored XSS. Try posting: &lt;script&gt;alert(\'XSS\')&lt;/script&gt;',
                author: 'user',
                date: new Date().toISOString()
            }
        ];

        await put('db/articles/data.json', JSON.stringify(articles), {
            access: 'public',
            addRandomSuffix: false
        });

        // Create banner configuration with your uploaded image
        const banners = [
            {
                id: 1,
                url: 'https://0zm03yaef8mnidwn.public.blob.vercel-storage.com/11.jpg',
                description: 'Default banner image'
            }
        ];

        await put('db/banners/data.json', JSON.stringify(banners), {
            access: 'public',
            addRandomSuffix: false
        });

        console.log('Database initialized successfully');
    } catch (error) {
        console.error('Database initialization error:', error);
    }
}

// Get all users
export async function getUsers() {
    try {
        const { blobs } = await list({ prefix: 'db/users/' });
        if (blobs.length === 0) {
            await initializeDatabase();
            return getUsers();
        }

        const response = await fetch(blobs[0].url);
        return await response.json();
    } catch (error) {
        console.error('Error fetching users:', error);
        return [];
    }
}

// Get all articles
export async function getArticles() {
    try {
        const { blobs } = await list({ prefix: 'db/articles/' });
        if (blobs.length === 0) {
            await initializeDatabase();
            return getArticles();
        }

        const response = await fetch(blobs[0].url);
        return await response.json();
    } catch (error) {
        console.error('Error fetching articles:', error);
        return [];
    }
}

// Update users
export async function updateUsers(users) {
    try {
        await put('db/users/data.json', JSON.stringify(users), {
            access: 'public',
            addRandomSuffix: false
        });
        return true;
    } catch (error) {
        console.error('Error updating users:', error);
        return false;
    }
}

// Update articles
export async function updateArticles(articles) {
    try {
        await put('db/articles/data.json', JSON.stringify(articles), {
            access: 'public',
            addRandomSuffix: false
        });
        return true;
    } catch (error) {
        console.error('Error updating articles:', error);
        return false;
    }
}

// VULNERABLE: SQL-like query simulation (intentionally insecure)
export function executeVulnerableQuery(table, condition) {
    // This simulates SQL injection by evaluating the condition string
    // In a real SQL database, this would be: SELECT * FROM table WHERE condition

    if (table === 'users') {
        return getUsers().then(users => {
            // VULNERABILITY: eval-like behavior
            try {
                return users.filter(user => {
                    // Simulate SQL WHERE clause evaluation
                    // This is intentionally vulnerable to injection
                    const evalCondition = condition
                        .replace(/username/g, `'${user.username}'`)
                        .replace(/password/g, `'${user.password}'`)
                        .replace(/role/g, `'${user.role}'`);

                    // Check for common SQL injection patterns
                    if (condition.includes("OR '1'='1'") || condition.includes('OR 1=1')) {
                        return true; // Bypass authentication
                    }

                    return eval(evalCondition);
                });
            } catch (e) {
                return [];
            }
        });
    }

    if (table === 'articles') {
        return getArticles().then(articles => {
            try {
                return articles.filter(article => {
                    const evalCondition = condition
                        .replace(/title/g, `'${article.title}'`)
                        .replace(/content/g, `'${article.content}'`)
                        .replace(/author/g, `'${article.author}'`);

                    if (condition.includes("OR '1'='1'") || condition.includes('OR 1=1')) {
                        return true;
                    }

                    return eval(evalCondition);
                });
            } catch (e) {
                return [];
            }
        });
    }

    return Promise.resolve([]);
}
