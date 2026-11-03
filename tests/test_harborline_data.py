import unittest
from tools.validate_harborline_data import validate

class HarborlineDataTests(unittest.TestCase):
    def test_project_data_contracts(self):
        self.assertEqual(validate(), [])

if __name__ == "__main__":
    unittest.main()
