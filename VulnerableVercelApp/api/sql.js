export default async function handler(req, res) {
    const { id } = req.query;

    // DEFAULT BANNER
    let banner = "https://0zm03yaef8mnidwn.public.blob.vercel-storage.com/11.jpg";
    let content = "Welcome to the vulnerable site. The banner above is loaded dynamically from the database.";
    let debugSql = "";

    // SIMULATED SQL INJECTION VULNERABILITY
    // Real SQL injection requires a real database. Since Vercel is stateless, we simulate the behavior.
    // We check if the input looks like a SQL payload.

    if (id) {
        // Construct the query string to show to the attacker (Reflected in debug)
        debugSql = `SELECT * FROM banners WHERE id = ${id}`;

        // Try to fetch from database first
        try {
            const { list } = await import('@vercel/blob');
            const { blobs } = await list({ prefix: 'db/banners/' });

            if (blobs.length > 0) {
                const response = await fetch(blobs[0].url);
                const banners = await response.json();
                const bannerData = banners.find(b => b.id == id);
                if (bannerData) {
                    banner = bannerData.url;
                }
            }
        } catch (e) {
            console.log('Database not initialized, using default banner');
        }

        // PATTERN 1: UNION SELECT Attack
        // Payload: -1 UNION SELECT 'https://attacker.com/pwned.jpg'
        const unionMatch = id.match(/UNION\s+SELECT\s+'([^']+)'/i);
        if (unionMatch) {
            banner = unionMatch[1]; // The injected URL becomes the banner
            content = "INJECTION SUCCESSFUL: Banner updated via UNION SELECT.";
        }

        // PATTERN 2: Boolean Blind (Simulation)
        // Payload: 1 OR 1=1
        else if (id.match(/OR\s+1=1/i) || id.match(/OR\s+'1'='1'/i)) {
            content = "INJECTION SUCCESSFUL: True statements exposed all records (Simulation).";
        }

        // PATTERN 3: Error Based (Simulation)
        // Payload: '
        else if (id.includes("'") && !unionMatch) {
            return res.status(500).json({
                error: "SQL Syntax Error",
                message: `You have an error in your SQL syntax; check the manual that corresponds to your MySQL server version for the right syntax to use near '${id}' at line 1`,
                sql: debugSql
            });
        }
    }

    res.status(200).json({
        id: id || 1,
        banner_url: banner,
        content: content,
        debug_query: debugSql
    });
}
