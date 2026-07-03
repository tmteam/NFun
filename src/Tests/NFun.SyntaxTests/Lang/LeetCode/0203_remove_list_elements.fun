# LeetCode 0203 — Remove Linked List Elements
#
# Build a new list omitting every node whose value equals `target`. Pure
# recursion — `target` instead of leetcode's `val` (val is reserved in NFun).

type node = {value: int, next: node? = none}

fun removeVal(head: node?, target) -> node?:
    if head == none: return none
    rest = removeVal(head.next, target)
    if head.value == target: return rest
    return node {value = head.value, next = rest}

fun toArray(n):
    out = []
    cur = n
    while cur != none:
        out = concat(out, [cur.value])
        cur = cur.next
    return out

@Test
fun testCanonical():
    head = node {value = 1, next =
            node {value = 2, next =
              node {value = 6, next =
                node {value = 3, next =
                  node {value = 4, next =
                    node {value = 5, next = node {value = 6}}}}}}}
    assertEqual(toArray(removeVal(head, 6)), [1, 2, 3, 4, 5])

@Test
fun testAllRemoved():
    head = node {value = 7, next = node {value = 7, next = node {value = 7}}}
    assertEqual(removeVal(head, 7) == none, true)

@Test
fun testEmpty():
    assertEqual(removeVal(none, 1) == none, true)
