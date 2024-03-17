import { readProject } from "./project.mjs"

// Main application

process.argv.forEach(function (val, index, array) {
    console.log(index + ': ' + val);
});

await readProject('sample/test1.ap', [])
