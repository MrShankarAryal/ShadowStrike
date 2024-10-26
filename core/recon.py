import socket
from scapy.all import sr1, IP, TCP

def scan_ports(target_ip):
    open_ports = []
    common_ports = [22, 80, 443, 8080, 3306]
    
    for port in common_ports:
        syn_packet = IP(dst=target_ip) / TCP(dport=port, flags="S")
        response = sr1(syn_packet, timeout=1, verbose=False)
        
        if response and response.haslayer(TCP) and response.getlayer(TCP).flags == 0x12:  # SYN-ACK
            open_ports.append(port)
    
    return open_ports

def gather_recon_data(target_ip):
    try:
        socket.inet_aton(target_ip)
        open_ports = scan_ports(target_ip)
        
        if not open_ports:
            print(f"No open ports detected on {target_ip}")
        else:
            print(f"Open ports found: {open_ports}")
        
        return open_ports
    except socket.error:
        print("Invalid IP address provided.")
        return []
