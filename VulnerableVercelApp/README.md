# VulnerableVercelApp

A **professional vulnerable web application** designed for security testing and penetration testing practice with [ShadowStrike](https://github.com/MrShankarAryal/ShadowStrike).

![Vulnerable Web App](https://img.shields.io/badge/Security-Vulnerable-red)
![Vercel](https://img.shields.io/badge/Vercel-Deployed-black)
![Database](https://img.shields.io/badge/Database-Blob%20Storage-blue)

## ⚠️ WARNING

**This application contains INTENTIONAL security vulnerabilities!**

- **DO NOT** deploy this in a production environment
- **DO NOT** use this with real user data
- **ONLY** use for authorized security testing and education
- The author is **NOT responsible** for any misuse

## 🎯 Purpose

This application serves as a realistic testing target for:
- Penetration testing tools (like ShadowStrike)
- Security training and education
- Vulnerability research
- Red team exercises

## 🔓 Vulnerabilities Included

### 1. SQL Injection
- **Login Bypass**: `admin' OR '1'='1' --`
- **Banner Defacement**: `-1 UNION SELECT 'url'`
- **Article Search**: Search parameter injection

### 2. Cross-Site Scripting (XSS)
- **Stored XSS**: Post articles with `<script>` tags
- **Reflected XSS**: Search functionality

### 3. Unrestricted File Upload
- No file type validation
- No size limits
- Direct storage to Vercel Blob

### 4. Broken Authentication
- Admin panel accessible without login
- Weak password hashing simulation

### 5. Sensitive Data Exposure
- Debug information in responses
- SQL queries exposed in JSON

## 🚀 Deployment

### Prerequisites
- Node.js 18+
- Vercel account
- Vercel Blob Storage configured

### Environment Setup

1. Create `.env.local` file:
```bash
BLOB_READ_WRITE_TOKEN=your_vercel_blob_token_here
```

2. Install dependencies:
```bash
npm install
```

3. Deploy to Vercel:
```bash
vercel
```

## 📚 API Endpoints

| Endpoint | Method | Vulnerability | Description |
|----------|--------|---------------|-------------|
| `/api/login` | POST | SQL Injection | User authentication |
| `/api/articles` | GET/POST | SQLi + XSS | Blog articles |
| `/api/admin` | GET | No Auth | Admin panel |
| `/api/upload` | POST | File Upload | File storage |
| `/api/sql` | GET | SQL Injection | Banner loader |

## 🧪 Testing with ShadowStrike

1. Deploy this app to Vercel
2. Run [ShadowStrike](https://github.com/MrShankarAryal/ShadowStrike)
3. Scan your Vercel URL
4. Execute Web Ransomware Kill Chain
5. Observe all 4 phases complete successfully

## 🛠️ Tech Stack

- **Frontend**: HTML5, CSS3, Vanilla JavaScript
- **Backend**: Vercel Serverless Functions (Node.js)
- **Database**: Vercel Blob Storage
- **Hosting**: Vercel

## 📖 Documentation

See [DEPLOYMENT.md](./DEPLOYMENT.md) for detailed deployment instructions.

## ⚖️ Legal Disclaimer

This software is provided for **educational purposes only**. By using this application, you agree to:

1. Only test against systems you own or have explicit permission to test
2. Comply with all applicable laws and regulations
3. Not use this for malicious purposes
4. Accept full responsibility for your actions

## 🤝 Contributing

This is a deliberately vulnerable application. If you find additional vulnerabilities or improvements:

1. Fork the repository
2. Create a feature branch
3. Submit a pull request

## 📄 License

MIT License - See [LICENSE](../LICENSE) file for details

## 🔗 Related Projects

- [ShadowStrike](https://github.com/MrShankarAryal/ShadowStrike) - Advanced penetration testing tool

## 👤 Author

**Shankar Aryal**
- GitHub: [@MrShankarAryal](https://github.com/MrShankarAryal)

---

**Remember**: With great power comes great responsibility. Use this tool ethically and legally.
# Testing123
>>>>>>> f7f73f57b02ac8773630a5b6c5feb89fad5d6f06
