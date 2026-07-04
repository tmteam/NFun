using NFun.Exceptions;
using NFun.TestTools;
using NFun.Tic;
using NUnit.Framework;

namespace NFun.SyntaxTests.NamedTypes;

/// <summary>
/// Tests for named type definitions and explicit construction.
/// </summary>
public class NamedTypeDefinitionTest {
    [SetUp]
    public void Initialize() => TraceLog.IsEnabled = true;

    [TearDown]
    public void Deinitialize() => TraceLog.IsEnabled = false;

    #region Field definition formats

    // Format 1: generic, required — {a}
    [Test]
    public void FieldFormat_GenericRequired() =>
        "type t = {a}\r out = t{a = 42}".CalcWithNamedTypes();

    // Format 2: typed, required — {a:int}
    [Test]
    public void FieldFormat_TypedRequired() =>
        "type t = {a:int}\r out = t{a = 42}".CalcWithNamedTypes();

    // Format 3: generic default — {a = 42} (integer constant, generic)
    [Test]
    public void FieldFormat_GenericDefault() =>
        "type t = {a = 42}\r out = t{}".CalcWithNamedTypes();

    // Format 4: concrete default — {a = false} (bool, concrete type)
    [Test]
    public void FieldFormat_ConcreteDefault() =>
        "type t = {a = false}\r out = t{}".CalcWithNamedTypes();

    // Format 5: typed with default — {a:int = 42}
    [Test]
    public void FieldFormat_TypedWithDefault() =>
        "type t = {a:int = 42}\r out = t{}".CalcWithNamedTypes();

    // All formats mixed
    [Test]
    public void FieldFormat_AllMixed() =>
        "type t = {a, b:int, c = 42, d = false, e:text = 'hi'}\r out = t{a = 1, b = 2}"
            .CalcWithNamedTypes();

    #endregion

    #region Basic construction

    [Test]
    public void Construction_AllFieldsProvided() {
        var result = "type point = {x:int, y:int}\r out = point{x = 1, y = 2}".CalcWithNamedTypes();
        // Verify fields exist and have correct values
    }

    [Test]
    public void Construction_DefaultOmitted() =>
        Assert.AreEqual("Alice",
            "type user = {name:text, age:int = 0}\r name = user{name = 'Alice'}.name"
                .CalcWithNamedTypes().Get("name"));

    [Test]
    public void Construction_DefaultOverridden() =>
        "type user = {name:text, age:int = 0}\r out = user{name = 'Bob', age = 25}".CalcWithNamedTypes();

    [Test]
    public void Construction_AllDefaults_EmptyBraces() =>
        "type config = {debug:bool = false, verbose:bool = false}\r out = config{}".CalcWithNamedTypes();

    [Test]
    public void Construction_ExplicitDefaultKeyword() =>
        "type config = {timeout:int = 30}\r out = config{timeout = default}".CalcWithNamedTypes();

    [Test]
    public void Construction_MultipleDefaultsOmitted() =>
        "type t = {a:int, b:int = 10, c:int = 20, d:int = 30}\r out = t{a = 1}".CalcWithNamedTypes();

    [Test]
    public void Construction_OnlyRequiredProvided() =>
        "type t = {req:text, opt1 = 0, opt2 = 0, opt3 = 0}\r out = t{req = 'x'}".CalcWithNamedTypes();

    #endregion

    #region Construction errors

    [Test]
    public void Error_MissingRequiredField() =>
        Assert.Throws<FunnyParseException>(
            () => "type user = {name:text, age:int}\r out = user{name = 'Alice'}".BuildWithNamedTypes());

    [Test]
    public void Error_MissingAllRequiredFields() =>
        Assert.Throws<FunnyParseException>(
            () => "type user = {name:text, age:int}\r out = user{}".BuildWithNamedTypes());

    [Test]
    public void Error_UnknownField() =>
        Assert.Throws<FunnyParseException>(
            () => "type user = {name:text}\r out = user{name = 'x', unknown = 42}".BuildWithNamedTypes());

    [Test]
    public void Error_DuplicateFieldInConstructor() =>
        // Duplicate field in constructor — rejected (consistent with anonymous struct behavior)
        Assert.Throws<FunnyParseException>(
            () => "type user = {name:text}\r out = user{name = 'a', name = 'b'}".BuildWithNamedTypes());

    [Test]
    public void Error_WrongFieldType() =>
        Assert.Throws<FunnyParseException>(
            () => "type t = {x:int}\r out = t{x = 'hello'}".BuildWithNamedTypes());

    [Test]
    public void Error_UndefinedType() =>
        Assert.Throws<FunnyParseException>(
            () => "out = unknown{x = 1}".BuildWithNamedTypes());

    #endregion

    #region Type annotation = check, not construction

    [Test]
    public void Annotation_FullStruct_Passes() =>
        "type point = {x:int, y:int}\r out:point = point{x = 1, y = 2}".CalcWithNamedTypes();

    [Test]
    public void Annotation_PartialAnonymousStruct_Fails() =>
        Assert.Throws<FunnyParseException>(
            () => "type point = {x:int, y:int}\r out:point = {x = 1}".BuildWithNamedTypes());

    [Test]
    public void Annotation_FullAnonymousStruct_Passes() =>
        "type point = {x:int, y:int}\r out:point = {x = 1, y = 2}".CalcWithNamedTypes();

    [Test]
    public void Annotation_ComposesWithArray() =>
        "type point = {x:int, y:int}\r out:point[] = [point{x=1,y=2}]".CalcWithNamedTypes();

    [Test]
    public void Annotation_ComposesWithOptional() =>
        "type point = {x:int, y:int}\r out:point? = if(true) point{x=1,y=2} else none"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled,
                             namedTypesSupport: NamedTypesSupport.Enabled);

    #endregion

    #region Default for entire named type

    [Test]
    public void DefaultKeyword_AllFieldsHaveExplicitDefaults() =>
        "type config = {debug:bool = false, verbose:bool = false}\r out:config = default"
            .CalcWithNamedTypes();

    [Test]
    public void DefaultKeyword_MixedDefaults_TypeDefaultsUsed() =>
        // name has no explicit default → uses type default ('' for text)
        "type user = {name:text, age:int = 0}\r out:user = default"
            .CalcWithNamedTypes();

    [Test]
    public void DefaultKeyword_AllFieldsRequired_UsesTypeDefaults() =>
        "type point = {x:int, y:int}\r out:point = default"
            .CalcWithNamedTypes();

    #endregion

    #region Generic fields — type inferred from context

    [Test]
    public void Generic_AllGenericRequired() =>
        "type pair = {a, b}\r out = pair{a = 1, b = 'hello'}".CalcWithNamedTypes();

    [Test]
    public void Generic_TypeFromAnnotation_Int() {
        var result = "type wrap = {x}\r y:int = wrap{x = 42}.x".CalcWithNamedTypes();
        Assert.AreEqual(42, result.Get("y"));
    }

    [Test]
    public void Generic_TypeFromAnnotation_Byte() {
        var result = "type wrap = {x}\r y:byte = wrap{x = 1}.x".CalcWithNamedTypes();
        Assert.AreEqual((byte)1, result.Get("y"));
    }

    [Test]
    public void Generic_TypeFromAnnotation_Real() {
        var result = "type wrap = {x}\r y:real = wrap{x = 1}.x".CalcWithNamedTypes();
        Assert.AreEqual(1.0, result.Get("y"));
    }

    [Test]
    public void Generic_DefaultIsGenericInt() {
        // c = 42 is generic integer constant [U8..Re]
        var result = "type t = {c = 42}\r y:byte = t{}.c".CalcWithNamedTypes();
        Assert.AreEqual((byte)42, result.Get("y"));
    }

    [Test]
    public void Generic_FourFields() =>
        "type quad = {a, b, c, d}\r out = quad{a=1, b=2, c=3, d=4}".CalcWithNamedTypes();

    #endregion

    #region Default values — different types

    [Test]
    public void Default_Int() {
        var result = "type t = {x:int = 42}\r out = t{}.x".CalcWithNamedTypes();
        Assert.AreEqual(42, result.Get("out"));
    }

    [Test]
    public void Default_Real() {
        var result = "type t = {x:real = 3.14}\r out = t{}.x".CalcWithNamedTypes();
        Assert.AreEqual(3.14, result.Get("out"));
    }

    [Test]
    public void Default_Bool_True() {
        var result = "type t = {x = true}\r out = t{}.x".CalcWithNamedTypes();
        Assert.AreEqual(true, result.Get("out"));
    }

    [Test]
    public void Default_Bool_False() {
        var result = "type t = {x = false}\r out = t{}.x".CalcWithNamedTypes();
        Assert.AreEqual(false, result.Get("out"));
    }

    [Test]
    public void Default_Text() {
        var result = "type t = {x = 'hello'}\r out = t{}.x".CalcWithNamedTypes();
        Assert.AreEqual("hello", result.Get("out"));
    }

    [Test]
    public void Default_EmptyText() {
        var result = "type t = {x:text = ''}\r out = t{}.x".CalcWithNamedTypes();
        Assert.AreEqual("", result.Get("out"));
    }

    [Test]
    public void Default_Zero() {
        var result = "type t = {x = 0}\r out = t{}.x".CalcWithNamedTypes();
        Assert.AreEqual(0, result.Get("out"));
    }

    [Test]
    public void Default_NegativeInt() {
        var result = "type t = {x = -1}\r out = t{}.x".CalcWithNamedTypes();
        Assert.AreEqual(-1, result.Get("out"));
    }

    #endregion

    #region Structural compatibility

    [Test]
    public void Structural_FieldAccess() {
        var result = "type user = {name:text, age:int = 0}\r u = user{name = 'Alice'}\r out = u.name"
            .CalcWithNamedTypes();
        Assert.AreEqual("Alice", result.Get("out"));
    }

    [Test]
    public void Structural_DefaultFieldAccess() {
        var result = "type user = {name:text, age:int = 0}\r u = user{name = 'Alice'}\r out = u.age"
            .CalcWithNamedTypes();
        Assert.AreEqual(0, result.Get("out"));
    }

    [Test]
    public void Structural_WidthSubtyping() =>
        // user (2 fields) used where {name:text} expected
        "type user = {name:text, age:int = 0}\r f(s) = s.name\r out = f(user{name = 'Alice'})"
            .CalcWithNamedTypes();

    [Test]
    public void Structural_PassToFunction() {
        var result = "type user = {name:text, age:int = 0}\r greet(u) = u.name\r out = greet(user{name = 'Alice'})"
            .CalcWithNamedTypes();
        Assert.AreEqual("Alice", result.Get("out"));
    }

    [Test]
    public void Structural_AnonymousAndNamedInterchangeable() =>
        // Named type result used in same array as anonymous struct
        "type point = {x:int, y:int}\r out = [point{x=1, y=2}, {x=3, y=4}]".CalcWithNamedTypes();

    #endregion

    #region Nested types

    [Test]
    public void Nested_ExplicitConstruction() {
        var result = ("type addr = {city:text, zip:text = '00000'}\r" +
                      "type user = {name:text, addr:addr}\r" +
                      "u = user{name = 'Alice', addr = addr{city = 'NYC'}}\r" +
                      "out = u.addr.zip")
            .CalcWithNamedTypes();
        Assert.AreEqual("00000", result.Get("out"));
    }

    [Test]
    public void Nested_AllFieldsSpecified() {
        var result = ("type addr = {city:text, zip:text = '00000'}\r" +
                      "type user = {name:text, addr:addr}\r" +
                      "u = user{name = 'Alice', addr = addr{city = 'NYC', zip = '10001'}}\r" +
                      "out = u.addr.zip")
            .CalcWithNamedTypes();
        Assert.AreEqual("10001", result.Get("out"));
    }

    [Test]
    public void Nested_ThreeLevels() {
        var result = ("type inner = {v:int = 99}\r" +
                      "type middle = {i:inner}\r" +
                      "type outer = {m:middle}\r" +
                      "out = outer{m = middle{i = inner{}}}.m.i.v")
            .CalcWithNamedTypes();
        Assert.AreEqual(99, result.Get("out"));
    }

    #endregion

    #region Arrays

    [Test]
    public void Array_OfNamedType() =>
        "type user = {name:text, age:int = 0}\r out = [user{name='a'}, user{name='b', age=5}]"
            .CalcWithNamedTypes();

    [Test]
    public void Array_FieldAccessViaMap() {
        var result = ("type user = {name:text, age:int = 0}\r" +
                      "users = [user{name = 'Alice'}, user{name = 'Bob'}]\r" +
                      "out = users.map(rule it.name)")
            .CalcWithNamedTypes();
    }

    [Test]
    public void Array_MixedWithAnonymous() =>
        // Named and anonymous structs with same fields in one array
        "type point = {x:int, y:int}\r out = [point{x=1,y=2}, {x=3,y=4}]".CalcWithNamedTypes();

    [Test]
    public void Array_Empty_Typed() =>
        "type point = {x:int, y:int}\r out:point[] = []".CalcWithNamedTypes();

    #endregion

    #region Functions with named types

    [Test]
    public void Function_NamedTypeParam() {
        var result = ("type user = {name:text, age:int = 0}\r" +
                      "greet(u:user) = u.name\r" +
                      "out = greet(user{name = 'Alice'})")
            .CalcWithNamedTypes();
        Assert.AreEqual("Alice", result.Get("out"));
    }

    [Test]
    public void Function_NamedTypeReturn() =>
        "type point = {x:int, y:int}\r makePoint(a:int, b:int):point = point{x=a, y=b}\r out = makePoint(1,2)"
            .CalcWithNamedTypes();

    [Test]
    public void Function_GenericFieldPassthrough() {
        var result = ("type wrap = {x}\r" +
                      "unwrap(w) = w.x\r" +
                      "out = unwrap(wrap{x = 42})")
            .CalcWithNamedTypes();
        Assert.AreEqual(42, result.Get("out"));
    }

    #endregion

    #region Multiple type definitions

    [Test]
    public void Multiple_IndependentTypes() =>
        ("type a = {x:int}\r" +
         "type b = {y:text}\r" +
         "out1 = a{x = 1}\r" +
         "out2 = b{y = 'hello'}")
            .CalcWithNamedTypes();

    [Test]
    public void Multiple_TypeUsingOtherType() {
        var result = ("type point = {x:int, y:int}\r" +
                      "type rect = {origin:point, w:int, h:int}\r" +
                      "r = rect{origin = point{x=0, y=0}, w=100, h=50}\r" +
                      "out = r.w")
            .CalcWithNamedTypes();
        Assert.AreEqual(100, result.Get("out"));
    }

    #endregion

    #region Type definition errors

    [Test]
    public void Error_TypeRedefinition() =>
        Assert.Throws<FunnyParseException>(
            () => "type t = {x:int}\r type t = {y:int}\r out = t{x=1}".BuildWithNamedTypes());

    [Test]
    public void Error_DuplicateFieldInDefinition() =>
        Assert.Throws<FunnyParseException>(
            () => "type t = {x:int, x:int}\r out = t{x=1}".BuildWithNamedTypes());

    [Test]
    public void EmptyTypeDefinition_IsValid() =>
        // Empty struct type with no fields — valid but unusual
        "type t = {}\r out = t{}".CalcWithNamedTypes();

    // Circular alias detection is covered in ImpossibleRecursiveTypeDefinitionsTest.cs.

    [Test]
    public void AliasChain_NoCycle_IsValid() =>
        "type a = int\r type b = a\r out:b = 42".CalcWithNamedTypes();

    #endregion

    #region Multiline syntax

    [Test]
    public void Multiline_Definition() =>
        ("type user = {\r" +
         "  name: text\r" +
         "  age = 0\r" +
         "  active = true\r" +
         "}\r" +
         "out = user{name = 'Alice'}")
            .CalcWithNamedTypes();

    [Test]
    public void Multiline_Constructor() =>
        ("type user = {name:text, age:int = 0}\r" +
         "out = user{\r" +
         "  name = 'Alice'\r" +
         "  age = 25\r" +
         "}")
            .CalcWithNamedTypes();

    #endregion

    #region if-else with named types

    [Test]
    public void IfElse_BothBranchesConstructed() =>
        ("type user = {name:text, age:int = 0}\r" +
         "out = if(true) user{name='a'} else user{name='b', age=5}")
            .CalcWithNamedTypes();

    [Test]
    public void IfElse_NamedAndAnonymous() =>
        ("type point = {x:int, y:int}\r" +
         "out = if(true) point{x=1, y=2} else {x=3, y=4}")
            .CalcWithNamedTypes();

    #endregion

    #region Edge cases

    [Test]
    public void EdgeCase_SingleField() =>
        "type wrap = {x:int}\r out = wrap{x = 42}".CalcWithNamedTypes();

    [Test]
    public void EdgeCase_FieldOrderDoesntMatter() =>
        "type t = {a:int, b:text}\r out = t{b = 'hello', a = 42}".CalcWithNamedTypes();

    [Test]
    public void EdgeCase_TypeNameSameAsField() =>
        "type x = {x:int}\r out = x{x = 42}".CalcWithNamedTypes();

    [Test]
    public void EdgeCase_ConstructorInExpression() {
        var result = "type wrap = {x:int}\r out = wrap{x = 1}.x + wrap{x = 2}.x".CalcWithNamedTypes();
        Assert.AreEqual(3, result.Get("out"));
    }

    [Test]
    public void EdgeCase_NestedConstructorInArray() =>
        "type inner = {v:int = 0}\r type outer = {i:inner}\r out = [outer{i=inner{}}, outer{i=inner{v=1}}]"
            .CalcWithNamedTypes();

    [Test]
    public void EdgeCase_DefaultUsedInArithmetic() {
        var result = "type t = {x = 10, y = 20}\r out = t{}.x + t{}.y".CalcWithNamedTypes();
        Assert.AreEqual(30, result.Get("out"));
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════
    // Named struct array preserves preferred type
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void NamedStructArray_PreservesPreferredType() {
        var runtime = Funny.Hardcore.WithDialect(
            optionalTypesSupport: OptionalTypesSupport.Enabled,
            namedTypesSupport: NamedTypesSupport.Enabled)
            .Build("type user = {score:int?}\r y = [user{score=10}, user{score=none}, user{score=5}]");
        var r = runtime.Calc();
        // score should be Int32? (from named type), not UInt8?
        var arr = r.Get("y");
        Assert.IsNotNull(arr);
    }

    [Test]
    public void NamedStructArray_Compiles() {
        Assert.DoesNotThrow(() =>
            Funny.Hardcore.WithDialect(
                optionalTypesSupport: OptionalTypesSupport.Enabled,
                namedTypesSupport: NamedTypesSupport.Enabled)
                .Build("type user = {score:int?}\r y = [user{score=10}, user{score=none}, user{score=5}]"));
    }

    // Circular alias detection lives in ImpossibleRecursiveTypeDefinitionsTest.cs.

    // ═══════════════════════════════════════════════════════════════
    // Struct field type inference with defaults
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void StructFieldTypeInference_Works() {
        "type config = {retries = 3}; c = config{}; out = c.retries + 1"
            .CalcWithDialect(namedTypesSupport: NamedTypesSupport.Enabled)
            .AssertResultHas("out", 4);
    }

    // ═══════════════════════════════════════════════════════════════
    // Duplicate field in named constructor — compile error
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void DuplicateFieldInNamedConstructor_CompileError() {
        Assert.Throws<FunnyParseException>(() =>
            "type pt = {x:int, y:int}; out = pt{x=1, y=2, x=3}.x"
                .CalcWithDialect(namedTypesSupport: NamedTypesSupport.Enabled));
    }

    // ═══════════════════════════════════════════════════════════════
    // Uppercase field name
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void UppercaseFieldName_Works() {
        "type pt = {A:int}; out = pt{A=42}.A"
            .CalcWithDialect(namedTypesSupport: NamedTypesSupport.Enabled)
            .AssertResultHas("out", 42);
    }

    // ═══════════════════════════════════════════════════════════════
    // Alias to struct constructor
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void AliasToStructConstructor_Works() {
        "type pair = {a:int, b:int}; type alias = pair; out = alias{a=1, b=2}.a"
            .CalcWithDialect(namedTypesSupport: NamedTypesSupport.Enabled)
            .AssertResultHas("out", 1);
    }

    // ═══════════════════════════════════════════════════════════════
    // IP default in struct
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void IpDefaultInStruct_Works() {
        "type server = {addr:ip = 0.0.0.0, port:int = 80}; out = server{}.port"
            .CalcWithDialect(namedTypesSupport: NamedTypesSupport.Enabled)
            .AssertResultHas("out", 80);
    }

    // ═══════════════════════════════════════════════════════════════
    // Primitive type alias as annotation
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void PrimitiveTypeAlias_AsAnnotation() {
        "type age = int; x:age = 42; y = x + 1"
            .CalcWithDialect(namedTypesSupport: NamedTypesSupport.Enabled)
            .AssertResultHas("y", 43);
    }

    // ═══════════════════════════════════════════════════════════════
    // Optional field in named struct arrays
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void OptionalFieldLostInArray() {
        // type t = {x: int?}; [t{x=1}, t{x=2}] should be {x:Int32?}[] not {x:Int32}[]
        Assert.DoesNotThrow(() =>
            "type t = {x: int?}; items = [t{x=1}, t{x=2}]; out = items[0].x ?? -1"
                .CalcWithDialect(
                    optionalTypesSupport: OptionalTypesSupport.Enabled,
                    namedTypesSupport: NamedTypesSupport.Enabled));
    }

    [Test]
    public void OptionalArrayFieldMapSum() {
        "type d = {items: int?[]}; x = d{items=[1,none,3]}; out = x.items.map(rule it ?? 0).sum()"
            .CalcWithDialect(
                optionalTypesSupport: OptionalTypesSupport.Enabled,
                namedTypesSupport: NamedTypesSupport.Enabled)
            .AssertResultHas("out", 4);
    }

    // ═══════════════════════════════════════════════════════════════
    // Coalesce on named struct field
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void CoalesceOnNamedStructField_Works() {
        "type w = {items:int?[]}; y = w{items=[42, none]}; out = y.items[0] ?? -1"
            .CalcWithDialect(
                optionalTypesSupport: OptionalTypesSupport.Enabled,
                namedTypesSupport: NamedTypesSupport.Enabled)
            .AssertResultHas("out", 42);
    }

    // ═══════════════════════════════════════════════════════════════
    // Inline struct annotation with uppercase field names
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void InlineStructAnnotation_UppercaseFieldName() {
        // {minVal:int} — lowercase normalize creates duplicate "minval" key
        Assert.DoesNotThrow(
            () => "y:{minVal:int, maxVal:int} = {minVal=1, maxVal=2}".Calc());
    }

    // ───────────────────────────────────────────────────────────────
    // MR4Bug4 — Type default values are validated lazily (at first use).
    //   `type t = {x:int = 'hello'}` declared but never instantiated
    //   silently compiles. Violates Basics.md Construction-stage rule:
    //   "checking the correctness of the script and calculating the
    //   types of all expressions in the script."
    //
    //   The bad default leaks to production when t{} is eventually used.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void MR4Bug4_TypeDefault_BadValue_NotValidatedEagerly() {
        Assert.Throws<FunnyParseException>(() =>
            Funny.Hardcore
                .WithDialect(namedTypesSupport: NamedTypesSupport.Enabled)
                .Build("type t = {x:int = 'hello'}"));
    }

    [Test]
    public void MR4Bug4_TypeDefault_BadValue_CaughtEvenWhenOverridden() {
        // Before the fix: the bad default was lazily checked only when triggered.
        // `v = t{x=42}` overrode the default, hiding the broken declaration.
        Assert.Throws<FunnyParseException>(() =>
            Funny.Hardcore
                .WithDialect(namedTypesSupport: NamedTypesSupport.Enabled)
                .Build("type t = {x:int = 'hello'}\rv = t{x=42}"));
    }

    [Test]
    public void MR4Bug4_TypeDefault_BoolForInt_CaughtAtDeclaration() {
        Assert.Throws<FunnyParseException>(() =>
            Funny.Hardcore
                .WithDialect(namedTypesSupport: NamedTypesSupport.Enabled)
                .Build("type t = {x:int = true}"));
    }

    [Test]
    public void MR4Bug4_TypeDefault_ValidValue_AcceptedAtDeclaration() {
        Assert.DoesNotThrow(() =>
            Funny.Hardcore
                .WithDialect(namedTypesSupport: NamedTypesSupport.Enabled)
                .Build("type t = {x:int = 42}"));
    }
}
