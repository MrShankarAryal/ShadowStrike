import json
import yaml

def create_targets_json():
    targets = {
        "targets": [
            {
                "name": "Default Target 1",
                "ip": "192.168.1.100",
                "port": 80,
                "attack_mode": "SYN Flood"
            },
            {
                "name": "Default Target 2",
                "url": "http://example.com",
                "attack_mode": "HTTP DDoS"
            }
        ]
    }
    
    with open('config/targets.json', 'w') as f:
        json.dump(targets, f, indent=4)

def create_attack_config_yaml():
    config = {
        "attack_settings": {
            "default_ports": [80, 443],
            "default_thread_count": 100,
            "attack_modes": ["Automatic", "SYN Flood", "HTTP DDoS", "Stealth Probe", "Custom Exploit"],
            "logging": {
                "enabled": True,
                "log_file": "attack_log.txt",
                "log_level": "INFO"
            }
        },
        "network_settings": {
            "max_bandwidth_usage": "100MBps",
            "packet_interval": 0.1
        },
        "gui_settings": {
            "appearance_mode": "System",
            "theme": "dark-blue"
        }
    }

    with open('config/attack_config.yaml', 'w') as f:
        yaml.dump(config, f)

if __name__ == "__main__":
    create_targets_json()
    create_attack_config_yaml()
    print("Configuration files have been set up.")
