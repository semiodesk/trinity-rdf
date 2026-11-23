"""Tests for stores.py, run by `python3 -m unittest discover -s .github/scripts`.

`images()` reads each suite's pinned images out of its C# fixture with a regex. When Testcontainers 4
moved the image into the builder's constructor, that regex would have stopped matching without a word:
check.sh would have skipped its "image not pulled" pre-check and let the suite pull gigabytes with its
output hidden. These tests make a spelling the regex misses fail here instead.
"""
import unittest

from stores import IMAGE, STORES, images


class FindsEveryPinnedImage(unittest.TestCase):
    def test_every_suite_pins_an_image(self):
        for store in STORES:
            with self.subTest(store=store):
                self.assertTrue(images(store), f"no container image found in tests/Trinity.Tests.{store}/*.cs")

    def test_every_image_is_pinned_to_a_tag(self):
        # The fixtures pin exact versions so an upstream push to :latest cannot redden a branch that
        # changed nothing; an untagged image means :latest.
        for store in STORES:
            for image in images(store):
                with self.subTest(store=store, image=image):
                    name, _, tag = image.rpartition(":")
                    self.assertTrue(name and "/" not in tag, f"{image} has no tag")
                    self.assertNotEqual(tag, "latest")

    def test_both_builder_spellings(self):
        source = '''
            new ContainerBuilder("a/b:1.0")
            new ContainerBuilder().WithImage("c/d:2.0")
            new ContainerBuilder(image)
        '''
        self.assertEqual(IMAGE.findall(source), ["a/b:1.0", "c/d:2.0"])


if __name__ == "__main__":
    unittest.main()
