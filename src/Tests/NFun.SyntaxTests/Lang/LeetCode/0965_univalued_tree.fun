# LeetCode 0965 — Univalued Binary Tree
#
# All nodes share the same value. Recurse: every existing child must match
# its parent's value AND itself be a univalued subtree.

type tree = {value: int, left: tree? = none, right: tree? = none}

fun isUnivalued(t):
    if t == none: return true
    if t.left != none and t.left.value != t.value: return false
    if t.right != none and t.right.value != t.value: return false
    return isUnivalued(t.left) and isUnivalued(t.right)

@Test
fun testEmpty():
    assertEqual(isUnivalued(none), true)

@Test
fun testSingle():
    assertEqual(isUnivalued(tree {value = 1}), true)

@Test
fun testAllOnes():
    root = tree {
        value = 1
        left = tree {
            value = 1
            left = tree {value = 1}
            right = tree {value = 1}
        }
        right = tree {
            value = 1
            right = tree {value = 1}
        }
    }
    assertEqual(isUnivalued(root), true)

@Test
fun testMixed():
    root = tree {
        value = 2
        left = tree {value = 2}
        right = tree {value = 5}
    }
    assertEqual(isUnivalued(root), false)
