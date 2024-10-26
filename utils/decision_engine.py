def decide_attack_mode(target_info):
    if target_info.get("url"):
        return "HTTP DDoS"
    elif target_info.get("ip"):
        if target_info.get("ports_to_scan"):
            return "Stealth Probe"
        else:
            return "SYN Flood"
    else:
        return "Custom Exploit"

if __name__ == "__main__":
    target1 = {"ip": "192.168.1.1", "ports_to_scan": [22, 80, 443]}
    print(decide_attack_mode(target1))  # Stealth Probe

    target2 = {"url": "http://example.com"}
    print(decide_attack_mode(target2))  # HTTP DDoS
