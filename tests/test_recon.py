import unittest
from core.stealth_probe import stealth_probe

class TestRecon(unittest.TestCase):

    def test_stealth_probe(self):
        result = stealth_probe(target_ip="192.168.1.100", ports=[22, 80, 443])
        self.assertIsInstance(result, list)

if __name__ == "__main__":
    unittest.main()
