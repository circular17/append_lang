# build custom peg grammar
if [ "generated/custom-peg.mjs" -ot "custom-peg.pegjs" ]; then
    echo "Building custom PEG grammar..."
    peggy -o generated/custom-peg.mjs --format es custom-peg.pegjs
    node grammar-preprocessor.mjs grammar.custom.pegjs generated/grammar.out.pegjs
else
    echo "No need to rebuild custom PEG grammar"
    # convert custom peg to pegjs
    if [ "generated/grammar.out.pegjs" -ot "grammar.custom.pegjs" ]; then
        echo "Preprocessing append grammar..."
        node grammar-preprocessor.mjs grammar.custom.pegjs generated/grammar.out.pegjs
    else
        echo "No need to preprocess append grammar"
    fi
fi

# build append grammar
if [ "generated/grammar.out.mjs" -ot "generated/grammar.out.pegjs" ]; then
    echo "Building append grammar..."
    peggy -o generated/grammar.out.mjs -d tree:../tree.mjs --format es generated/grammar.out.pegjs
    # import all tree
    perl -i -pe 's|import tree from|import * as tree from|' generated/grammar.out.mjs
else
    echo "No need to build append grammar"
fi