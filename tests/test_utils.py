import unittest
from utils.ip_utils import is_valid_ip, is_private_ip
from utils.decision_engine import decide_attack_mode

class TestUtils(unittest.TestCase):

    def test_is_valid_ip(self):
        self.assertTrue(is_valid_ip("192.168.1.1"))
        self.assertFalse(is_valid_ip("999.999.999.999"))

    def test_is_private_ip(self):
        self.assertTrue(is_private_ip("192.168.1.1"))
        self.assertFalse(is_private_ip("8.8.8.8"))

    def test_decide_attack_mode(self):
        target = {"ip": "192.168.1.1", "ports_to_scan": [22, 80, 443]}
        self.assertEqual(decide_attack_mode(target), "Stealth Probe")

if __name__ == "__main__":
    unittest.main()
