# LeetCode 0925 — Long Pressed Name
#
# Two pointers: advance in `name` on match; otherwise the `typed` char must
# equal the previous `typed` char (a long-press), or the input is invalid.

fun isLongPressedName(name, typed):
    i = 0
    j = 0
    while j < typed.count():
        if i < name.count() and name[i] == typed[j]:
            i += 1
            j += 1
        elif j > 0 and typed[j] == typed[j - 1]:
            j += 1
        else:
            return false
    return i == name.count()

@Test('alex', 'aaleex', true)
@Test('saeed', 'ssaaedd', false)
@Test('leelee', 'lleeelee', true)
@Test('laiden', 'laiden', true)
fun testIsLongPressedName(name, typed, expected):
    assertEqual(isLongPressedName(name, typed), expected)
