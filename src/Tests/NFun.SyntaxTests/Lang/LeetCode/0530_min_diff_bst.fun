# LeetCode 0530 — Minimum Absolute Difference in BST
#
# Smallest absolute diff between any two node values. Inorder of a BST is
# sorted — so the minimum diff is between adjacent inorder values. Single
# pass after building the sorted list via concat-recursion.

type tree = {value: int, left: tree? = none, right: tree? = none}

fun inorder(t):
    if t == none: return []
    return concat(concat(inorder(t.left), [t.value]), inorder(t.right))

fun minDiffInBST(t):
    seq = inorder(t)
    n = seq.count()
    best = 2147483647
    i = 1
    while i < n:
        d = seq[i] - seq[i - 1]
        if d < best: best = d
        i += 1
    return best

@Test
fun testCanonical():
    #     4
    #    / \
    #   2   6
    #  / \
    # 1   3      inorder = [1,2,3,4,6]  min diff = 1
    root = tree {
        value = 4
        left = tree {
            value = 2
            left = tree {value = 1}
            right = tree {value = 3}
        }
        right = tree {value = 6}
    }
    assertEqual(minDiffInBST(root), 1)

@Test
fun testSparse():
    # inorder = [1, 11, 100]   min diff = 10
    root = tree {
        value = 11
        left = tree {value = 1}
        right = tree {value = 100}
    }
    assertEqual(minDiffInBST(root), 10)
