// Main application

const fs = require('fs');
const {transpile} = require("./transpiler");

process.argv.forEach(function (val, index, array) {
    console.log(index + ': ' + val);
});

fs.readFile('sample.ap', 'utf8', (err, data) => {
    if (err) {
        console.error(err)
        return
    }
    transpile(data)
});

