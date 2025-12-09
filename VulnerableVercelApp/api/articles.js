import { getArticles, updateArticles, executeVulnerableQuery } from './db.js';

export default async function handler(req, res) {
    // GET: Fetch articles (with optional vulnerable search)
    if (req.method === 'GET') {
        const { search, id } = req.query;

        try {
            if (search) {
                // VULNERABILITY: SQL Injection in search
                const condition = `title LIKE '%${search}%' OR content LIKE '%${search}%'`;
                const articles = await executeVulnerableQuery('articles', condition);

                return res.status(200).json({
                    articles,
                    debug_query: condition
                });
            }

            if (id) {
                // VULNERABILITY: SQL Injection in ID lookup
                const condition = `id = ${id}`;
                const articles = await executeVulnerableQuery('articles', condition);

                return res.status(200).json({
                    article: articles[0] || null,
                    debug_query: condition
                });
            }

            // Return all articles
            const articles = await getArticles();
            return res.status(200).json({ articles });

        } catch (error) {
            return res.status(500).json({
                error: 'Database error',
                message: error.message
            });
        }
    }

    // POST: Create new article
    if (req.method === 'POST') {
        const { title, content, author } = req.body;

        if (!title || !content) {
            return res.status(400).json({ error: 'Title and content required' });
        }

        try {
            const articles = await getArticles();
            const newArticle = {
                id: articles.length + 1,
                title,
                content, // VULNERABILITY: Stored XSS - no sanitization
                author: author || 'anonymous',
                date: new Date().toISOString()
            };

            articles.push(newArticle);
            await updateArticles(articles);

            return res.status(201).json({
                success: true,
                article: newArticle,
                warning: 'Content stored without sanitization (XSS vulnerable)'
            });
        } catch (error) {
            return res.status(500).json({
                error: 'Database error',
                message: error.message
            });
        }
    }

    return res.status(405).json({ error: 'Method not allowed' });
}
