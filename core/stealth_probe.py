from scapy.all import IP, TCP, sr1

def stealth_probe(target_ip, ports):
    print(f"[+] Starting Stealth Probe on {target_ip}")
    
    for port in ports:
        syn_packet = IP(dst=target_ip) / TCP(dport=port, flags="S")
        response = sr1(syn_packet, timeout=1, verbose=False)
        
        if response and response.haslayer(TCP) and response.getlayer(TCP).flags == 0x12:
            print(f"[+] Port {port} is open on {target_ip}")
        else:
            print(f"[-] Port {port} is closed or filtered on {target_ip}")

if __name__ == "__main__":
    target_ip = input("Enter target IP: ")
    ports = list(map(int, input("Enter ports to scan (comma-separated, e.g., 22,80,443): ").split(',')))
    stealth_probe(target_ip, ports)
