import os, sys, pickle as pkl, shutil as sh, json
from PIL import Image
from AnyQt.QtWidgets import *
from AnyQt.QtGui import *
from collections import OrderedDict
import hashlib

def insert (source_str, insert_str, pos):
    return source_str[:pos]+insert_str+source_str[pos:]

iext = [".tif",'.tiff','.png','.jpg','.jpeg','.gif']
def loadFns(dirn,ign="",recurse="yes"):
	if ign == ";":
		ign = ""
	list = []
	for subdir, dirs, files in os.walk(dirn,topdown = True):
		for file in files:
			if os.path.splitext(file)[1] in iext:
				if ign == "" or ign not in os.path.join(subdir, file):
					list.append(os.path.join(subdir, file))
		if recurse=="no":
			break;
	return list

class DisplayWindow(QWidget):
	
	def __init__(self, files, targets, names):
		super().__init__()
		
		self.namemap = names
		self.targets = targets
		self.files = files
		self.pos = -1
		self.max = len(files)
		self.output = {k:[] for k in targets}
		
		self.fsets = {k:[] for k in files}
		
		self.curiset = []
		self.curf = ""
		
		self.initUI()
		
	def initUI(self):
	
		self.prev = QPushButton("Previous")
		self.prev.clicked.connect(self.toprev)
		self.prev.setShortcut(QKeySequence("Left"))
		self.next = QPushButton("Next")
		self.next.clicked.connect(self.tonext)
		self.next.setShortcut(QKeySequence("Right"))

		self.poslbl = QLabel(self)
		self.poslbl.setText("?/?")
		
		self.btns = QHBoxLayout()
		self.btns.addWidget(self.poslbl)
		self.btns.addStretch(1)
		self.btns.addWidget(self.prev)
		self.btns.addWidget(self.next)

		self.img = QPixmap(100,100)
		self.img.fill(QColor(0,0,0,63))
		self.imglbl = QLabel(self)
		self.imglbl.setPixmap(self.img)
		
		self.selgrid = QGridLayout()
		self.selgrid.setSpacing(2)
		sgh = QVBoxLayout()
		sgh.addLayout(self.selgrid)
		sgh.addStretch(1)
		
		x = 0
		y = 0
		self.selbtns = {}
		for k in self.targets:
			pos = (x,y)
			x+=1
			if x >= 20:
				y+=1
				x=0
				
			name = os.path.basename(k)
			if name in self.namemap:
				name = self.namemap[name]
			
			btn = QPushButton(name, self)
			btn.setCheckable(True)
			btn.clicked[bool].connect(self.btnsel)
			
			if "&" in name:
				chr = name[name.find("&")+1].upper()
				btn.setShortcut(QKeySequence(chr))
			
			btn.target = k
			
			self.selbtns[k] = btn
			self.selgrid.addWidget(btn,*pos)
			
		self.select = QHBoxLayout()
		self.select.addWidget(self.imglbl)
		self.select.addStretch(1)
		self.select.addLayout(sgh)
		
		
		self.vbox = QVBoxLayout()
		self.vbox.addLayout(self.select)
		self.vbox.addStretch(1)
		self.vbox.addLayout(self.btns)
		
		self.setLayout(self.vbox) 
		
		self.resize(10,10) # shrink to min size
		self.center()
		self.setWindowTitle('')	 
		self.show()
		
	def tonext(self):
		self.mvImg(1)
	def toprev(self):
		self.mvImg(-1)
		
	def mvImg(self,incr):
		self.pos+=incr
		if self.pos < 0:
			self.pos = 0
		if self.pos >= self.max:
			self.pos = self.max-1
		
		if self.curf != "":
			self.fsets[self.curf] = self.curiset
		
		self.curf = self.files[self.pos]
		self.curiset = self.fsets[self.curf]
		
		self.setWindowTitle(os.path.basename(self.curf))
		
		for k,v in self.selbtns.items():
			v.setChecked(k in self.curiset)
		
		self.poslbl.setText("{}/{}".format(self.pos+1,self.max));
		
		self.next.show()
		self.prev.show()
		if self.pos == 0:
			self.prev.hide()
		if self.pos == self.max-1:
			self.next.hide()
			
		if self.curf.endswith("gif"):
			mov = QMovie(self.curf)
			self.imglbl.setMovie(mov)
			mov.start()
		else:
			img = QImage()
			img.load(self.curf)
			
			mw = 1000
			mh = 600
			if img.height() > mh:
				img = img.scaledToHeight(mh)
			if img.width() > mw:
				img = img.scaledToWidth(mw)
			self.img.convertFromImage(img)
			self.imglbl.setPixmap(self.img)
		
		if not self.isMaximized():
			self.resize(10,10)
		
	def center(self):
		qr = self.frameGeometry()
		cp = QDesktopWidget().availableGeometry().center()
		qr.moveCenter(cp)
		self.move(qr.topLeft())
		
	def btnsel(self,state):
		el = self.sender()
		if state:
			self.curiset.append(el.target)
		else:
			self.curiset.remove(el.target)
			
	def closeEvent(self, event):
		ndone = sum([1 for k,v in self.fsets.items() if len(v) == 0])
	
		accept = ndone == 0
	
		if ndone > 0:
			reply = QMessageBox.question(self, 'Message',
				"Are you sure to quit? You still have {} more to fill out.".format(ndone), QMessageBox.Yes | 
				QMessageBox.No, QMessageBox.No)

			if reply == QMessageBox.Yes:
				event.accept()
				accept = True
			else:
				event.ignore()
				accept = False

				self.pos = self.files.index([k for k,v in self.fsets.items() if len(v) == 0][0])
				self.mvImg(0)
		
		if accept:
			if ndone > 0:
				self.incomplete = True
			else:
				self.incomplete = False
			for k,v in self.fsets.items():
				for i in v:
					self.output[i].append(k)

	def onclose(self):
		pass
			
if __name__ == "__main__":
	print("Locating Files...")
	fs = loadFns(sys.argv[3],"","no")
	keymap = json.load(open(sys.argv[2],"r"),object_pairs_hook=OrderedDict)
	tgdir = []
	for subdir, dirs, files in os.walk(sys.argv[1],topdown = True):
		tgdir = [os.path.join(subdir,k) for k in dirs]
		break
	
	fn = ".incomplete_data"
	
	sha_1 = hashlib.sha1()
	sha_1.update(" ".join(sys.argv).encode("UTF-8"))
	fnc = sha_1.hexdigest() + fn
	
	data = ({},0)
	if os.path.isfile(fn):
		data = pkl.load(open(fn,"rb"))
		os.remove(fn)
	if os.path.isfile(fnc):
		data = pkl.load(open(fnc,"rb"))
	
	if sys.argv[1] == sys.argv[3]:
		for d in tgdir:
			for subdir, dirs, files in os.walk(d,topdown = True):
				for f in files:
					n = os.path.join(sys.argv[3],f)
					if n not in data[0]:
						data[0][n] = []
					data[0][n].append(d)
				break
	else:
		fdata = json.load(open("cats.json.php","r"))
		for k,v in fdata.items():
			if k.startswith("__"):
				continue
			entr = os.path.join(sys.argv[1],k.split(":")[1])
			for f in v:
				n = os.path.join(sys.argv[3],f)
				if n in fs:
					if n not in data[0]:
						data[0][n] = []
					data[0][n].append(entr)
	
	
	if len(fs) > 0:
		a = QApplication(sys.argv)
		w = DisplayWindow(fs, tgdir, keymap)
		
		if data != 0:
			w.fsets = {**w.fsets,**data[0]}
			w.pos = data[1]-1
		
		w.mvImg(1)
		
		a.exec_()
		
		if w.incomplete:
			pkl.dump([w.fsets,w.pos],open(fnc,"wb"))
		else:
			pkl.dump([w.fsets,w.pos],open(fnc+".backup","wb"))
			if os.path.isfile(fnc):
				os.remove(fnc)
			data = w.output
			for k,a in data.items():
				for v in a:
					try:
						os.link(v,os.path.join(k,os.path.basename(v)))
					except FileExistsError:
						pass