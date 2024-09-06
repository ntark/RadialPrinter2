const fs = require('fs');
const path = require('path');
const express = require('express');

const app = express();

app.set('view engine', 'ejs');

app.use(express.static(path.join(__dirname, 'public')));
app.use(express.urlencoded({ extended: true }))

app.get('/', (req, res) => {
    res.render('home');
});

app.listen(5088, () => console.log("server has started"));
