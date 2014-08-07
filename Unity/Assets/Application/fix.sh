rm splash*.png
rm splash*.jpg

convert art.jpg -gravity center -thumbnail 480x800^  -extent  480x800 splash.jpg
convert art.jpg -gravity center -thumbnail 432x164^  -extent  432x164 splash_desktop.jpg
convert art.jpg -gravity center -thumbnail 128x128^  -extent  128x128 icon.png
convert art.jpg -gravity center -thumbnail 1024x768^ -extent 1024x768 splash_ipad_landscape.png
convert art.jpg -gravity center -thumbnail 768x1024^ -extent 768x1024 splash_ipad_portrait.png
convert art.jpg -gravity center -thumbnail 640x960^  -extent  640x960 splash_iphone_portrait_high.png
convert art.jpg -gravity center -thumbnail 640x1136^ -extent 640x1136 splash_iphone_tall_portrait.png

