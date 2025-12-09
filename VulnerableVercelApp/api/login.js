import { getUsers } from './db.js';

export default async function handler(req, res) {
    if (req.method !== 'POST') {
        return res.status(405).json({ error: 'Method not allowed' });
    }

    const { username, password } = req.body;

    if (!username || !password) {
        return res.status(400).json({
            success: false,
            error: 'Username and password required'
        });
    }

    try {
        // VULNERABILITY: SQL Injection with Password Bypass
        // This simulates a real-world vulnerable query like:
        // SELECT * FROM users WHERE username = '$username' AND password = MD5('$password')

        // In a real app, password would be hashed, but SQL injection can bypass this!
        // Attack vectors that work:
        // 1. Comment injection: admin' --
        // 2. OR injection: admin' OR '1'='1' --
        // 3. UNION injection: ' UNION SELECT * FROM users WHERE username='admin' --

        const users = await getUsers();

        // Simulate vulnerable SQL query construction
        const query = `SELECT * FROM users WHERE username = '${username}' AND password = MD5('${password}')`;
        console.log(`[VULNERABLE QUERY] ${query}`);

        // Check for SQL injection patterns
        let authenticatedUser = null;

        // Pattern 1: Comment-based injection (bypasses password check)
        // Example: admin' --
        if (username.includes("'") && (username.includes('--') || username.includes('#'))) {
            const cleanUsername = username.split("'")[0];
            authenticatedUser = users.find(u => u.username === cleanUsername);

            if (authenticatedUser) {
                return res.status(200).json({
                    success: true,
                    message: 'Login successful (SQL Injection: Comment bypass)',
                    user: {
                        id: authenticatedUser.id,
                        username: authenticatedUser.username,
                        role: authenticatedUser.role,
                        email: authenticatedUser.email
                    },
                    token: 'fake-jwt-token-' + Math.random().toString(36),
                    debug: {
                        query: query,
                        attack_type: 'Comment-based SQL Injection',
                        bypassed: 'Password check completely bypassed'
                    }
                });
            }
        }

        // Pattern 2: OR-based injection (returns first user)
        // Example: admin' OR '1'='1' --
        if (username.includes("OR") && username.includes("'1'='1'")) {
            authenticatedUser = users[0]; // Return first user (usually admin)

            return res.status(200).json({
                success: true,
                message: 'Login successful (SQL Injection: OR bypass)',
                user: {
                    id: authenticatedUser.id,
                    username: authenticatedUser.username,
                    role: authenticatedUser.role,
                    email: authenticatedUser.email
                },
                token: 'fake-jwt-token-' + Math.random().toString(36),
                debug: {
                    query: query,
                    attack_type: 'OR-based SQL Injection',
                    bypassed: 'Authentication logic completely bypassed'
                }
            });
        }

        // Pattern 3: UNION-based injection
        // Example: ' UNION SELECT 1,'admin','admin123','admin','admin@test.com' --
        if (username.includes('UNION') && username.includes('SELECT')) {
            return res.status(200).json({
                success: true,
                message: 'Login successful (SQL Injection: UNION attack)',
                user: {
                    id: 1,
                    username: 'admin',
                    role: 'admin',
                    email: 'admin@vulnerable.com'
                },
                token: 'fake-jwt-token-' + Math.random().toString(36),
                debug: {
                    query: query,
                    attack_type: 'UNION-based SQL Injection',
                    bypassed: 'Injected custom user data'
                }
            });
        }

        // Pattern 4: Time-based blind SQLi (simulation)
        // Example: admin' AND SLEEP(5) --
        if (username.includes('SLEEP') || username.includes('WAITFOR')) {
            return res.status(200).json({
                success: false,
                message: 'Time-based blind SQL injection detected',
                debug: {
                    query: query,
                    attack_type: 'Time-based Blind SQL Injection',
                    note: 'In a real scenario, this would cause a delay'
                }
            });
        }

        // Normal authentication (no injection)
        authenticatedUser = users.find(u =>
            u.username === username && u.password === password
        );

        if (authenticatedUser) {
            return res.status(200).json({
                success: true,
                message: 'Login successful',
                user: {
                    id: authenticatedUser.id,
                    username: authenticatedUser.username,
                    role: authenticatedUser.role,
                    email: authenticatedUser.email
                },
                token: 'fake-jwt-token-' + Math.random().toString(36),
                debug: {
                    query: query,
                    attack_type: 'None (legitimate login)'
                }
            });
        }

        // Failed login
        return res.status(401).json({
            success: false,
            error: 'Invalid credentials',
            debug: {
                query: query,
                hints: [
                    "Try: admin' --",
                    "Try: admin' OR '1'='1' --",
                    "Try: ' UNION SELECT 1,'admin','pass','admin','email' --"
                ]
            }
        });

    } catch (error) {
        return res.status(500).json({
            success: false,
            error: 'Database error',
            message: error.message,
            stack: error.stack
        });
    }
}
