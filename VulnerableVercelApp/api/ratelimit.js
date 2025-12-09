// Simple in-memory rate limiting (bypassable)
const rateLimitStore = new Map();

export default async function handler(req, res) {
    const ip = req.headers['x-forwarded-for'] || req.connection.remoteAddress;
    const userAgent = req.headers['user-agent'];

    // ✅ LOOKS SECURE: Rate limiting implemented
    const key = `${ip}-${userAgent}`;
    const now = Date.now();
    const windowMs = 60000; // 1 minute
    const maxRequests = 100;

    if (!rateLimitStore.has(key)) {
        rateLimitStore.set(key, { count: 1, resetTime: now + windowMs });
    } else {
        const record = rateLimitStore.get(key);

        if (now > record.resetTime) {
            // Reset window
            record.count = 1;
            record.resetTime = now + windowMs;
        } else {
            record.count++;

            if (record.count > maxRequests) {
                // ❌ VULNERABILITY: Can bypass by changing User-Agent or X-Forwarded-For header
                return res.status(429).json({
                    error: 'Too many requests',
                    retryAfter: Math.ceil((record.resetTime - now) / 1000),
                    vulnerability: 'Bypass hint: Change User-Agent or X-Forwarded-For header'
                });
            }
        }
    }

    // Clean up old entries
    if (rateLimitStore.size > 10000) {
        const entries = Array.from(rateLimitStore.entries());
        entries.forEach(([k, v]) => {
            if (now > v.resetTime) {
                rateLimitStore.delete(k);
            }
        });
    }

    return res.status(200).json({
        success: true,
        message: 'Rate limit check passed',
        remaining: maxRequests - (rateLimitStore.get(key)?.count || 0),
        resetIn: Math.ceil(((rateLimitStore.get(key)?.resetTime || now) - now) / 1000)
    });
}
