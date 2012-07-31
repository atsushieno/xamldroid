VERSION=0.1
DISTNAME=xamldroid-$(VERSION)
CSCOMPILE=dmcs
CSCOMPILE_FLAGS=-debug
RUNTIME=mono
RUNTIME_FLAGS=--debug

SOURCES= xamldroid.cs xamldroid-generator.cs
OTHERFILES= Makefile README
DIST_DEPS = MonoDroid.Xaml.dll
OUTCOME = Mono.Android.Xaml.dll
MSCORLIB_ANDROID = $(MONO_ANDROID_PATH)/lib/mono/2.1/mscorlib.dll
SYSTEM_ANDROID = $(MONO_ANDROID_PATH)/lib/mono/2.1/System.dll
SYSTEMXML_ANDROID = $(MONO_ANDROID_PATH)/lib/mono/2.1/System.Xml.dll
MONO_ANDROID_DLL = $(MONO_ANDROID_PATH)/lib/mandroid/platforms/android-15/Mono.Android.dll

all: $(OUTCOME)

xamldroid-generator.exe: xamldroid-generator.cs IKVM.Reflection.dll
	$(CSCOMPILE) $(CSCOMPILE_FLAGS) xamldroid-generator.cs -r:IKVM.Reflection.dll

IKVM.Reflection.dll:
	cd IKVM.Reflection && xbuild && cd .. || exit 1
	cp IKVM.Reflection/bin/Debug/IKVM.Reflection.dll .

xamldroid.generated.cs : xamldroid-generator.exe $(Mono_Android_dll)
	$(RUNTIME) $(RUNTIME_FLAGS) ./xamldroid-generator.exe $(MSCORLIB_ANDROID) $(SYSTEM_ANDROID) $(SYSTEMXML_ANDROID) $(MONO_ANDROID_DLL)

Mono.Android.Xaml.dll : xamldroid.cs xamldroid.generated.cs
	$(CSCOMPILE) $(CSCOMPILE_FLAGS) -debug -t:library -out:Mono.Android.Xaml.dll xamldroid.cs xamldroid.generated.cs -r:$(MSCORLIB_ANDROID) -r:$(SYSTEM_ANDROID) -r:$(SYSTEMXML_ANDROID) -r:$(MONO_ANDROID_DLL) -r:MonoDroid.Xaml.dll

clean:
	rm Mono.Android.Xaml.dll Mono.Android.Xaml.dll.mdb xamldroid.generated.cs xamldroid-generator.exe xamldroid-generator.exe.mdb

dist:
	mkdir $(DISTNAME)
	cp $(OUTCOME) $(DIST_DEPS) $(SOURCES) $(OTHERFILES) $(DISTNAME)
	tar jcf $(DISTNAME).tar.bz2 $(DISTNAME)

cleanup-dist:
	rm -rf $(DISTNAME).tar.bz2 $(DISTNAME)

