# LeetCode 1108 — Defanging an IP Address

fun defangIPaddr(address):
    out = ''
    for c in address:
        if c == /'.':
            out = concat(out, '[.]')
        else:
            out = concat(out, '{c}')
    return out

@Test('1.1.1.1', '1[.]1[.]1[.]1')
@Test('255.100.50.0', '255[.]100[.]50[.]0')
fun testDefang(addr, expected):
    assertEqual(defangIPaddr(addr), expected)
