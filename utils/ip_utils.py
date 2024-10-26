import re

def is_valid_ip(ip):
    pattern = re.compile(
        r"^(?:[0-9]{1,3}\.){3}[0-9]{1,3}$"
    )
    return pattern.match(ip) is not None

def is_private_ip(ip):
    private_ranges = [
        re.compile(r"^10\..*"),
        re.compile(r"^192\.168\..*"),
        re.compile(r"^172\.(1[6-9]|2[0-9]|3[0-1])\..*")
    ]
    return any(pattern.match(ip) for pattern in private_ranges)

if __name__ == "__main__":
    print(is_valid_ip("192.168.0.1"))  # True
    print(is_private_ip("192.168.0.1"))  # True
