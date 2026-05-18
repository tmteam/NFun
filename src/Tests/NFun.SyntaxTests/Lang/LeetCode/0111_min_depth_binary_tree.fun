# LeetCode 0111 — Minimum Depth of Binary Tree
#
# Length of the shortest path from root to LEAF. Subtle: if a node has one
# missing child, depth is `1 + depth(other)`, not `1 + 0` — we don't count
# the empty side as a leaf.

type tree = {value: int, left: tree? = none, right: tree? = none}

fun minDepth(t):
    if t == none: return 0
    if t.left == none: return 1 + minDepth(t.right)
    if t.right == none: return 1 + minDepth(t.left)
    return 1 + min(minDepth(t.left), minDepth(t.right))

@Test
fun testEmpty():
    assertEqual(minDepth(none), 0)

@Test
fun testLeaf():
    assertEqual(minDepth(tree {value = 1}), 1)

@Test
fun testOneSideSkew():
    # 1 → left=2 → left=3   min depth = 3 (no early leaf)
    root = tree {
        value = 1
        left = tree {
            value = 2
            left = tree {value = 3}
        }
    }
    assertEqual(minDepth(root), 3)

@Test
fun testBalanced():
    root = tree {
        value = 2
        left = tree {value = 1}
        right = tree {value = 3}
    }
    assertEqual(minDepth(root), 2)
