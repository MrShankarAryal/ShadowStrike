import unittest
from core.auto_attack import start_attack

class TestAutoAttack(unittest.TestCase):

    def test_start_attack(self):
        result = start_attack(target_ip="192.168.1.100", mode="SYN Flood")
        self.assertTrue(result)

if __name__ == "__main__":
    unittest.main()
