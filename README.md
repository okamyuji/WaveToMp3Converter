# Wave to MP3 Converter

�������ᕉ�ׂ�WAVE�t�@�C������MP3�ւ̕ϊ��c�[���ł��B�R���\�[���A�v���P�[�V������Windows�T�[�r�X�̗����Ƃ��ē��삵�܂��B

## ����

- **�����ϊ�**: NAudio��LAME���g�p���������I�ȃI�[�f�B�I�ϊ�
- **�Ⴡ��������**: �X�g���[�������ɂ�钀���ϊ��Ń������g�p�ʂ�}��
- **���@�\**: �P��t�@�C���ϊ��A�o�b�`�ϊ��A�t�H���_�Ď��@�\�𓋍�
- **�f���A�����[�h**: �R���\�[���A�v���P�[�V������Windows�T�[�r�X�̗����œ���
- **�ڍׂȃ��O**: �t�@�C�����O��Windows�C�x���g���O�̗����ɑΉ�

## �V�X�e���v��

- .NET Framework 4.8.1
- Windows 7�ȍ~

## �g�p���@

### �R���\�[���A�v���P�[�V�����Ƃ���

#### �P��t�@�C���̕ϊ�

```
WaveToMp3Converter.exe -f "C:\Music\song.wav"
```

�o�̓t�@�C���͓����f�B���N�g���ɍ쐬����܂��B�ʂ̏o�͐���w�肷��ꍇ�� `-o` �I�v�V�������g�p���܂��F

```
WaveToMp3Converter.exe -f "C:\Music\song.wav" -o "C:\MP3s"
```

#### �f�B���N�g�����̂��ׂĂ�WAVE�t�@�C����ϊ�

```
WaveToMp3Converter.exe -d "C:\Music" -o "C:\MP3s"
```

�T�u�f�B���N�g�����܂߂ď�������A�f�B���N�g���\���͕ێ�����܂��B

#### �t�H���_�������x�|�[�����O�ŊĎ����Ď����ϊ�

```
WaveToMp3Converter.exe -w "C:\WatchFolder" -o "C:\OutputFolder"
```

�w�肵���t�H���_�������x�^�C�}�[�ɂ��|�[�����O�ŊĎ����A�V����WAVE�t�@�C�����ǉ������Ǝ����I�ɕϊ����܂��B�t�@�C���̎�肱�ڂ����������ɂ������肵���Ď������ł��BCtrl+C�ŊĎ����I���ł��܂��B

#### �R�}���h���C���I�v�V�����ꗗ

| �I�v�V���� | ���� |
|------------|------|
| `-f, --file <�t�@�C���p�X>` | �ϊ�����P���WAVE�t�@�C�����w�� |
| `-d, --directory <�f�B���N�g���p�X>` | �w�肳�ꂽ�f�B���N�g�����̂��ׂĂ�WAVE�t�@�C����ϊ� |
| `-o, --output <�o�̓f�B���N�g��>` | �ϊ����ꂽMP3�t�@�C���̏o�͐�f�B���N�g�����w�� |
| `-w, --watch <�Ď��f�B���N�g��>` | �w�肳�ꂽ�f�B���N�g�����Ď����A�V����WAVE�t�@�C���������ϊ� |
| `-h, --help` | �w���v��\�� |

### Windows�T�[�r�X�Ƃ���

#### �ݒ�

�T�[�r�X�Ƃ��Ď��s����O�ɁA`WaveToMp3Converter.exe.config`�t�@�C���Ɉȉ��̐ݒ��ǉ����Ă��������F

```xml
<configuration>
  <appSettings>
    <add key="WatchDirectory" value="C:\WatchFolder" />
    <add key="OutputDirectory" value="C:\OutputFolder" />
  </appSettings>
</configuration>
```

#### �T�[�r�X�̃C���X�g�[��

�Ǘ��Ҍ����ŃR�}���h�v�����v�g���J���A�ȉ��̃R�}���h�����s���܂��F

```
sc create WaveToMp3Converter binPath= "���S�ȃp�X\WaveToMp3Converter.exe" DisplayName= "Wave to MP3 Converter Service"
```

#### �T�[�r�X�̊J�n/��~

�T�[�r�X�́AWindows�̃T�[�r�X�Ǘ��R���\�[���iservices.msc�j����J�n�E��~�ł��܂��B�܂��́A�ȉ��̃R�}���h���g�p���܂��F

```
sc start WaveToMp3Converter
sc stop WaveToMp3Converter
```

#### �T�[�r�X�̃A���C���X�g�[��

```
sc delete WaveToMp3Converter
```

## �Z�p�I�ȏڍ�

### �X�g���[�������ɂ���

���̃c�[���́A�t�@�C���S�̂���x�Ƀ������ɓǂݍ��ނ̂ł͂Ȃ��A�X�g���[���������g�p���Ē����I�ɕϊ����s���܂��F

1. �Œ�T�C�Y�̃o�b�t�@�i4096�o�C�g�j���g�p����WAVE�t�@�C���������ȃ`�����N�œǂݍ���
2. �ǂݍ��񂾃`�����N��������MP3�ɃG���R�[�h���ďo��
3. ������J��Ԃ����ƂŁA�������g�p�ʂ��ŏ����ɗ}���Ȃ���ϊ����s��

���̃A�v���[�`�ɂ��A�ȉ��̃����b�g������܂��F

- �������g�p�ʂ����ł���A�傫�ȃt�@�C���ł��������s���ɂȂ�ɂ���
- �����̐i���󋵂����A���^�C���ŕ\���\
- �V�X�e�����\�[�X�������I�Ɏg�p

### ���O�@�\

���O�͈ȉ���2�̏ꏊ�ɋL�^����܂��F

1. �t�@�C�����O: �A�v���P�[�V�����Ɠ����f�B���N�g����`WaveToMp3Converter.log`�Ƃ��ĕۑ�
2. Windows�C�x���g���O: �A�v���P�[�V�������O�ɁuWaveToMp3Converter�v�\�[�X�Ƃ��ċL�^�i�T�[�r�X���[�h���j

## �g���u���V���[�e�B���O

### �u�t�@�C���ɃA�N�Z�X�ł��܂���v�G���[

���̃A�v���P�[�V�����Ńt�@�C�����J����Ă���\��������܂��B���̃t�@�C������Ă���Ď��s���Ă��������B

### �ϊ����x���ꍇ

- CPU�̕��ׂ��������̃A�v���P�[�V�������I������
- �n�[�h�f�B�X�N�̋󂫗e�ʂ��\���ł��邱�Ƃ��m�F����
- �ꎞ�I�Ƀ��A���^�C���E�C���X�X�L�����𖳌��ɂ���

### �T�[�r�X���N�����Ȃ��ꍇ

- App.config�̐ݒ肪���������m�F����
- ���O�t�@�C���ŃG���[���b�Z�[�W���m�F����
- �w�肵���f�B���N�g���ւ̃A�N�Z�X�������邱�Ƃ��m�F����

## ���C�Z���X

���̃v���W�F�N�g��[MIT���C�Z���X](LICENSE)�̉��Œ񋟂���Ă��܂��B

## �ӎ�

���̃v���W�F�N�g�͈ȉ��̃I�[�v���\�[�X���C�u�������g�p���Ă��܂��F

- [NAudio](https://github.com/naudio/NAudio)
- [NAudio.Lame](https://github.com/Corey-M/NAudio.Lame)

## �X�V����

### v1.0.0 (2025-03-24)
- ���񃊃��[�X