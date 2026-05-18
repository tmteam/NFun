# LeetCode 0145 — Binary Tree Postorder Traversal
#
# Left, right, self.

type tree = {value: int, left: tree? = none, right: tree? = none}

fun postorder(t):
    if t == none: return []
    return concat(concat(postorder(t.left), postorder(t.right)), [t.value])

@Test
fun testEmpty():
    assertEqual(postorder(none), [])

@Test
fun testFullSmall():
    root = tree {
        value = 2
        left = tree {value = 1}
        right = tree {value = 3}
    }
    assertEqual(postorder(root), [1, 3, 2])

@Test
fun testRightSkew():
    root = tree {
        value = 1
        right = tree {
            value = 2
            left = tree {value = 3}
        }
    }
    assertEqual(postorder(root), [3, 2, 1])
