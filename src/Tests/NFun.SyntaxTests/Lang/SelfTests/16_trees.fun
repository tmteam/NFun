# 16_trees.fun - binary tree algorithms (array-heap representation)
# Tree as array: index 0 = root, left(i) = 2i+1, right(i) = 2i+2
# -1 = no node at that position
# Example: [1, 2, 3, 4, 5, -1, -1] =
#        1
#       / \
#      2   3
#     / \
#    4   5

fun leftIdx(i):
    return 2 * i + 1

fun rightIdx(i):
    return 2 * i + 2

fun hasNode(tree, i):
    if i >= tree.count(): return false
    return tree[i] != -1

# -- 1. Tree size --

fun treeSize(tree):
    count = 0
    for node in tree:
        if node != -1: count += 1
    return count

@Test
fun testTreeSize():
    assertEqual(treeSize([1]), 1)
    assertEqual(treeSize([1, 2, 3]), 3)
    assertEqual(treeSize([1, 2, 3, 4, 5, -1, -1]), 5)

# -- 2. Tree depth --

fun depthAt(tree, i):
    if not hasNode(tree, i): return 0
    ld = depthAt(tree, leftIdx(i))
    rd = depthAt(tree, rightIdx(i))
    return 1 + max(ld, rd)

@Test
fun testTreeDepth():
    assertEqual(depthAt([1], 0), 1)
    assertEqual(depthAt([1, 2, 3], 0), 2)
    assertEqual(depthAt([1, 2, -1, 3, -1, -1, -1], 0), 3)

# -- 3. Tree sum --

fun treeSum(tree):
    total = 0
    for node in tree:
        if node != -1: total += node
    return total

@Test
fun testTreeSum():
    assertEqual(treeSum([1, 2, 3]), 6)
    assertEqual(treeSum([10, 20, 30, 40]), 100)

# -- 4. Tree contains --

fun treeContains(tree, target):
    for node in tree:
        if node == target: return true
    return false

@Test(3, true)
@Test(99, false)
@Test(5, true)
fun testTreeContains(target, expected):
    tree = [1, 2, 3, 4, 5]
    assertEqual(treeContains(tree, target), expected)

# -- 5. Tree max --

fun treeMax(tree):
    m = tree[0]
    for node in tree:
        if node != -1 and node > m: m = node
    return m

@Test
fun testTreeMax():
    assertEqual(treeMax([5, 3, 8, 1, 4]), 8)
    assertEqual(treeMax([42]), 42)

# -- 6. Count leaves --

fun countLeaves(tree):
    count = 0
    i = 0
    while i < tree.count():
        if hasNode(tree, i):
            if not hasNode(tree, leftIdx(i)) and not hasNode(tree, rightIdx(i)):
                count += 1
        i += 1
    return count

@Test
fun testCountLeaves():
    assertEqual(countLeaves([1]), 1)
    assertEqual(countLeaves([1, 2, 3]), 2)
    assertEqual(countLeaves([1, 2, 3, 4, 5, -1, -1]), 3)

# -- 7. Level sum --

fun levelSum(tree, depth):
    total = 0
    start = 0
    d = 0
    while d < depth:
        start = 2 * start + 1
        d += 1
    width = 1
    d = 0
    while d < depth:
        width *= 2
        d += 1
    i = start
    while i < start + width and i < tree.count():
        if tree[i] != -1: total += tree[i]
        i += 1
    return total

@Test
fun testLevelSum():
    tree = [1, 2, 3, 4, 5, 6, 7]
    assertEqual(levelSum(tree, 0), 1)
    assertEqual(levelSum(tree, 1), 5)
    assertEqual(levelSum(tree, 2), 22)

# -- 8. Is complete --

fun isComplete(tree):
    foundGap = false
    for node in tree:
        if node == -1:
            foundGap = true
        elif foundGap:
            return false
    return true

@Test
fun testIsComplete():
    assertEqual(isComplete([1, 2, 3, 4, 5, 6]), true)
    assertEqual(isComplete([1, 2, 3, -1, -1, -1, 7]), false)

# -- 9. Count nodes at depth --

fun nodesAtDepth(tree, depth):
    count = 0
    start = 0
    d = 0
    while d < depth:
        start = 2 * start + 1
        d += 1
    width = 1
    d = 0
    while d < depth:
        width *= 2
        d += 1
    i = start
    while i < start + width and i < tree.count():
        if tree[i] != -1: count += 1
        i += 1
    return count

@Test(0, 1)
@Test(1, 2)
@Test(2, 4)
fun testNodesAtDepth(depth, expected):
    tree = [1, 2, 3, 4, 5, 6, 7]
    assertEqual(nodesAtDepth(tree, depth), expected)

# -- 10. Is valid BST --

fun isBstAt(tree, i, lo, hi):
    if not hasNode(tree, i): return true
    v = tree[i]
    if v <= lo or v >= hi: return false
    return isBstAt(tree, leftIdx(i), lo, v) and isBstAt(tree, rightIdx(i), v, hi)

fun isBst(tree):
    return isBstAt(tree, 0, -999999, 999999)

@Test
fun testIsBst():
    assertEqual(isBst([4, 2, 6, 1, 3, 5, 7]), true)
    assertEqual(isBst([4, 6, 2]), false)
    assertEqual(isBst([5]), true)
