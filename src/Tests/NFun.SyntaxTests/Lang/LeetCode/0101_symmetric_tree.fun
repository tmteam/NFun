# LeetCode 0101 — Symmetric Tree
#
# Is the binary tree a mirror image of itself around its center? Recurse on
# pairs (left/right) and (right/left).

type tree = {value: int, left: tree? = none, right: tree? = none}

fun isMirror(a, b):
    if a == none and b == none: return true
    if a == none or b == none: return false
    if a.value != b.value: return false
    return isMirror(a.left, b.right) and isMirror(a.right, b.left)

fun isSymmetric(root):
    if root == none: return true
    return isMirror(root.left, root.right)

@Test
fun testEmpty():
    assertEqual(isSymmetric(none), true)

@Test
fun testSingleNode():
    assertEqual(isSymmetric(tree {value = 1}), true)

@Test
fun testSymmetric():
    #       1
    #      / \
    #     2   2
    #    / \ / \
    #   3  4 4  3
    root = tree {
        value = 1
        left = tree {
            value = 2
            left = tree {value = 3}
            right = tree {value = 4}
        }
        right = tree {
            value = 2
            left = tree {value = 4}
            right = tree {value = 3}
        }
    }
    assertEqual(isSymmetric(root), true)

@Test
fun testAsymmetric():
    root = tree {
        value = 1
        left = tree {
            value = 2
            right = tree {value = 3}
        }
        right = tree {
            value = 2
            right = tree {value = 3}
        }
    }
    assertEqual(isSymmetric(root), false)
