# LeetCode 2236 — Root Equals Sum of Children
#
# Trivial three-node tree check.

type tree = {value: int, left: tree? = none, right: tree? = none}

fun checkTree(root):
    if root == none: return false
    # The problem guarantees both children exist on a non-empty input.
    return root.value == root.left!.value + root.right!.value

@Test
fun testTrue():
    root = tree {
        value = 10
        left = tree {value = 4}
        right = tree {value = 6}
    }
    assertEqual(checkTree(root), true)

@Test
fun testFalse():
    root = tree {
        value = 5
        left = tree {value = 3}
        right = tree {value = 1}
    }
    assertEqual(checkTree(root), false)
