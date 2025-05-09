import io
import re
import logging
import PyPDF2
import fitz  # PyMuPDF
from typing import Dict, List, Set, Tuple, Optional

class PDFAnonimleyici:
    """Basit yıldız maskeleme ile kaplama yaklaşımını kullanan gelişmiş PDF anonimleyici"""
    
    # Anonimleştirme yapılmayacak bölüm başlıkları
    HARIC_TUTULAN_BOLUMLER = [
        r'giriş',
        r'REFERENCES',
        r'references',
        r'ilgili\s*çalışmalar',
        r'referanslar',
        r'kaynakça',
        r'kaynaklar',
        r'teşekkür',
        r'ACKNOWLEDGMENT',
        r'acknowledgment'
        ,r'thanks',
        r'THANKS',
        r'INTRODUCTION',
        r'MATERAILS AND METHODS',
        r' I. INTRODUCTION',
        r' A. EMOTIONAL EVENT',
        r'II. DATASET ON EMOTION WITH NATURALISTICSTIMULI (DENS)',
        r' B. EXPERIMENTAL DETAILS',
        r'C. PREPROCESSINGANDARTIFACTREMOVALOFTHEEEGDATA',
        r' D. OTHERDATASETSUSED',
        r' III. METHODOLOGY',
        r' IV. RESULTS',
        r'ACKNOWLEDGMENT',
        r' VI. CONCLUSION',
        r'Abstract',
        r'ABSTRACT',
        r' A. CNN Model with FFT Feature Extraction ',
        r'EXPRIMENTAL RESULTS AND DISCUSSION',
        r'EMOTIONAL EVENT',
        r'CONCLUSION',
        r'Comparison between FFT and CWT Models ',        
    ]
    
    def __init__(self, pdf_akisi: io.BytesIO):
        """PDF içeriği ile başlat"""
        self.pdf_akisi = pdf_akisi
        self.pdf_akisi.seek(0)
        
        # Metadata çıkarımı için PyPDF2 ile okumayı dene
        try:
            self.okuyucu = PyPDF2.PdfReader(self.pdf_akisi)
            self.pdf_akisi.seek(0)  # Akış pozisyonunu sıfırla
            logging.info(f"PDF başarıyla PyPDF2 ile açıldı, {len(self.okuyucu.pages)} sayfa")
        except Exception as e:
            logging.error(f"PyPDF2 ile PDF açma hatası: {str(e)}")
            raise
            
        # Algılanan bilgi depolama
        self.yazar_isimleri = set()
        self.epostalar = set()
        self.kurumlar = set()
        self.haric_tutulan_bolgeler = []
        
        # Sayfa numarasına göre hariç tutulan sayfa bölgeleri
        self.haric_tutulan_sayfa_bolgeleri = {}
        
    def bilgi_cikar(self) -> Dict:
        """PDF'den yazar isimlerini, e-postaları ve kurumları çıkar"""
        logging.info("PDF metadata ve içeriği çıkarılıyor...")
        
        # PDF metadata çıkar
        metadata = {}
        try:
            if hasattr(self.okuyucu, 'metadata') and self.okuyucu.metadata:
                metadata = {
                    'author': self.okuyucu.metadata.author,
                    'creator': self.okuyucu.metadata.creator,
                    'producer': self.okuyucu.metadata.producer,
                    'title': self.okuyucu.metadata.title,
                    'subject': self.okuyucu.metadata.subject
                }
                logging.debug(f"PDF metadata: {metadata}")
        except Exception as e:
            logging.warning(f"Metadata çıkarma hatası: {str(e)}")
        
        # Tüm sayfalardan metin çıkar
        tum_metin = ""
        sayfa_metinleri = []
        sayfa_sayisi = len(self.okuyucu.pages)
        
        for i in range(sayfa_sayisi):
            try:
                sayfa = self.okuyucu.pages[i]
                metin = sayfa.extract_text()
                if metin:
                    tum_metin += metin + "\n"
                    sayfa_metinleri.append(metin)
                    logging.debug(f"Sayfa {i+1}: {len(metin)} karakter çıkarıldı")
                else:
                    sayfa_metinleri.append("")
                    logging.warning(f"Sayfa {i+1}: Metin çıkarılamadı veya boş")
            except Exception as e:
                sayfa_metinleri.append("")
                logging.error(f"Sayfa {i+1}'den metin çıkarma hatası: {str(e)}")
        
        # Anonimleştirilecek bilgileri bul
        yazar_isimleri = self._yazar_isimlerini_bul(tum_metin)
        epostalar = self._epostalari_bul(tum_metin)
        kurumlar = self._kurumlari_bul(tum_metin)
        
        # Hariç tutulan bölümleri tanımla (REFERANSLAR gibi)
        self._haric_tutulan_bolumleri_tanimla(sayfa_metinleri)
        
        # Bulguları sakla
        self.yazar_isimleri = yazar_isimleri
        self.epostalar = epostalar
        self.kurumlar = kurumlar
        
        return {
            'metadata': metadata,
            'yazar_isimleri': list(yazar_isimleri),
            'epostalar': list(epostalar),
            'kurumlar': list(kurumlar),
            'haric_tutulan_bolgeler': self.haric_tutulan_sayfa_bolgeleri
        }
        
    def anonimize_et(self,
                 isimleri_anonimize_et: bool = True,
                 epostalari_anonimize_et: bool = True,
                 kurumlari_anonimize_et: bool = True) -> io.BytesIO:
        """
        Basit yıldız maskeleme ile PyMuPDF kaplama yaklaşımını kullanarak PDF'i anonimize eder.
        """
        logging.info("Yıldız maskeleme ile PDF anonimizasyonu başlatılıyor...")
        
        # Bilgileri henüz yapılmadıysa çıkar
        if not self.yazar_isimleri and not self.epostalar and not self.kurumlar:
            self.bilgi_cikar()
            
        # Yıldızlarla anonimleştirme eşleştirmeleri oluştur
        isim_degisimleri = {}
        eposta_degisimleri = {}
        kurum_degisimleri = {}
        
        # Yıldız değişimlerini oluştur (orijinal metin ile aynı uzunlukta)
        if isimleri_anonimize_et:
            for isim in self.yazar_isimleri:
                isim_degisimleri[isim] = '*' * len(isim)
            logging.info(f"{len(isim_degisimleri)} yazar ismi yıldız maskesi oluşturuldu")
                
        if epostalari_anonimize_et:
            for eposta in self.epostalar:
                eposta_degisimleri[eposta] = '*' * len(eposta)
            logging.info(f"{len(eposta_degisimleri)} e-posta yıldız maskesi oluşturuldu")
                
        if kurumlari_anonimize_et:
            for kurum in self.kurumlar:
                kurum_degisimleri[kurum] = '*' * len(kurum)
            logging.info(f"{len(kurum_degisimleri)} kurum yıldız maskesi oluşturuldu")
        
        # Tüm değişimleri al
        tum_degisimler = {}
        tum_degisimler.update(isim_degisimleri if isimleri_anonimize_et else {})
        tum_degisimler.update(eposta_degisimleri if epostalari_anonimize_et else {})
        tum_degisimler.update(kurum_degisimleri if kurumlari_anonimize_et else {})
        
        # Akış pozisyonunu sıfırla
        self.pdf_akisi.seek(0)
        
        try:
            # Gerçek anonimleştirme için PyMuPDF (fitz) kullan
            cikis_akisi = io.BytesIO()
            
            # PyMuPDF ile PDF'i aç
            with fitz.open(stream=self.pdf_akisi.read(), filetype="pdf") as belge:
                toplam_degisimler = 0
                haric_tutulan_bolgelerde_atlanan = 0
                
                # Her sayfayı işle
                for sayfa_num in range(len(belge)):
                    sayfa = belge[sayfa_num]
                    sayfa_metni = sayfa.get_text()
                    sayfa_degisimleri = 0
                    
                    # Bu sayfa için hariç tutulan bölgeleri al
                    haric_tutulan_bolgeler = self.haric_tutulan_sayfa_bolgeleri.get(sayfa_num, [])
                    
                    # Anonimize edilecek tüm metin oluşumlarını bul
                    for orijinal_metin, degisim_metni in tum_degisimler.items():
                        # Çok kısa dizeleri (4 karakterden az) yanlış pozitifleri önlemek için atla
                        if len(orijinal_metin) < 4:
                            continue
                            
                        # Sayfadaki tüm oluşumları bul
                        for eslesme in sayfa.search_for(orijinal_metin):
                            # Eşleşmenin hariç tutulan bir bölgede olup olmadığını kontrol et
                            haric_tutulan_bolgede = False
                            
                            # Bu sayfada hariç tutulan bölgeler varsa, eşleşmenin bunlardan birinde olup olmadığını kontrol et
                            for bolge in haric_tutulan_bolgeler:
                                # Hariç tutulan bölge için bir dikdörtgen oluştur
                                bolge_dikdortgeni = fitz.Rect(bolge["x0"], bolge["y0"], bolge["x1"], bolge["y1"])
                                
                                # Eşleşme dikdörtgeninin hariç tutulan bölge ile kesişip kesişmediğini kontrol et
                                if eslesme.intersects(bolge_dikdortgeni):
                                    haric_tutulan_bolgede = True
                                    haric_tutulan_bolgelerde_atlanan += 1
                                    logging.debug(f"Hariç tutulan bölgedeki '{orijinal_metin}' metni atlanıyor")
                                    break
                            
                            # Hariç tutulan bölgedeyse atla
                            if haric_tutulan_bolgede:
                                continue
                                
                            # Orijinal metni kaplamak için beyaz dikdörtgen çiz
                            sayfa.draw_rect(eslesme, color=(1, 1, 1), fill=(1, 1, 1))
                            
                            # Yıldız değişimini ekle
                            metin_noktasi = fitz.Point(eslesme.x0, eslesme.y1 - 2)  # Metni dikdörtgenin altına konumlandır
                            sayfa.insert_text(metin_noktasi, degisim_metni, fontsize=8, color=(0, 0, 0))
                            
                            sayfa_degisimleri += 1
                    
                    logging.debug(f"Sayfa {sayfa_num+1}: {sayfa_degisimleri} anonimleştirme uygulandı")
                    toplam_degisimler += sayfa_degisimleri
                
                if isimleri_anonimize_et and belge.metadata:
                    metadata = dict(belge.metadata)
                    if 'author' in metadata and metadata['author']:
                        # Yazarı yıldızlarla değiştir
                        yazar_uzunlugu = len(metadata['author']) if metadata['author'] else 10
                        metadata['author'] = '*' * yazar_uzunlugu
                    belge.set_metadata(metadata)
                
                # Çıkış akışına kaydet
                belge.save(cikis_akisi)
                logging.info(f"Toplam {toplam_degisimler} değişim ile anonimleştirme tamamlandı")
                logging.info(f"Hariç tutulan bölgelerde {haric_tutulan_bolgelerde_atlanan} eşleşme atlandı")
            
            cikis_akisi.seek(0)
            return cikis_akisi
            
        except Exception as e:
            logging.error(f"PDF anonimleştirme sırasında hata: {str(e)}")
            self.pdf_akisi.seek(0)
            return self.pdf_akisi
            
    def _yazar_isimlerini_bul(self, metin: str) -> Set[str]:
        """Metindeki yazar isimlerini bul"""
        desenler = [
            r'(?:Author|Yazar|Authors)s?:?\s*([\w\s,\.]+)', 
            r'(?:İsim|Ad|Soyad)[\s:]+([\w\s]+)',
            r'([A-Z][a-z]+\s+[A-Z][a-z]+)(?:\s*,\s*\d)?',
            r'([A-Z][a-z]+\s+[A-Z]\.)\s',
            r'([A-Z][a-z]+\s+[A-Z][a-z\-]+)',
            r'([A-Z][a-z]+\s+[A-Z]\.\s+[A-Z][a-z]+)',
            r'([A-Z][a-z]+\s+et\s+al\.)',
            r'([A-Z][a-z]+\s+and\s+[A-Z][a-z]+)',
        ]
        
        yazarlar = set()
        for desen in desenler:
            eslesimler = re.finditer(desen, metin)
            for eslesim in eslesimler:
                yazar = eslesim.group(1).strip()
                if ' ' in yazar and len(yazar) > 4:
                    yazarlar.add(yazar)
                    logging.debug(f"Algılanan yazar ismi: '{yazar}'")
        
        return yazarlar
    
    def _epostalari_bul(self, metin: str) -> Set[str]:
        """Metindeki e-posta adreslerini bul"""
        eposta_deseni = r'[\w\.-]+@[\w\.-]+\.\w+(?:\.\w+)?'
        
        epostalar = set()
        eslesimler = re.finditer(eposta_deseni, metin)
        for eslesim in eslesimler:
            eposta = eslesim.group(0).strip()
            epostalar.add(eposta)
            logging.debug(f"Algılanan e-posta adresi: '{eposta}'")
            
        return epostalar
    
    def _kurumlari_bul(self, metin: str) -> Set[str]:
        """Metindeki kurum isimlerini bul"""
        uni_desenleri = [
            r'(\w+\s+Üniversitesi)',
            r'(\w+\s+University)',
            r'(İstanbul\s+\w+\s+Üniversitesi)',
            r'(Kocaeli\s+\w+\s+Üniversitesi)',
            r'(University\s+of\s+\w+)',
            r'(\w+\s+Institute\s+of\s+Technology)',
            r'(\w+\s+College)',
        ]
        
        diger_desenler = [
            r'([\w\s]+\s+Enstitüsü)',
            r'([\w\s]+\s+Fakültesi)',
            r'([\w\s]+\s+Bölümü)',
            r'([\w\s]+\s+Laboratuvarı)',
            r'([\w\s]+\s+Institute)',
            r'([\w\s]+\s+Department)',
            r'([\w\s]+\s+Araştırma Merkezi)',
            r'([\w\s]+\s+Research Center)',
            r'([\w\s]+\s+Corporation)',
            r'([\w\s]+\s+Inc\.)',
            r'([\w\s]+\s+Ltd\.)',
            r'([\w\s&]+\s+Company)',
        ]
        
        kurumlar = set()
        
        for desen in uni_desenleri + diger_desenler:
            eslesimler = re.finditer(desen, metin)
            for eslesim in eslesimler:
                kurum = eslesim.group(1).strip()
                if len(kurum) > 5:
                    kurumlar.add(kurum)
                    logging.debug(f"Algılanan kurum: '{kurum}'")
        
        return kurumlar
    
    def _haric_tutulan_bolumleri_tanimla(self, sayfa_metinleri: List[str]):
        """
        Her sayfada bölüm başlıklarını arayarak anonimleştirilmemesi gereken
        bölümleri tanımlar.
        
        Bu metod, PDF'deki hariç tutulan bölümleri (REFERANSLAR gibi) bulur
        ve içerikleri anonimleştirmekten kaçınmak için sınırlayıcı kutularını saklar.
        """
        logging.info("Anonimleştirme için atlanacak hariç tutulan bölümler tanımlanıyor...")
        self.haric_tutulan_sayfa_bolgeleri = {}
        
        # PDF akış pozisyonunu sıfırla
        self.pdf_akisi.seek(0)
        
        try:
            # Bölüm algılama için PyMuPDF ile PDF'i aç
            with fitz.open(stream=self.pdf_akisi.read(), filetype="pdf") as belge:
                # Her sayfa için
                for sayfa_num in range(len(belge)):
                    sayfa = belge[sayfa_num]
                    haric_tutulan_bolgeler = []
                    
                    for desen in self.HARIC_TUTULAN_BOLUMLER:
                        for bolum_deseni in [desen, desen.upper()]:
                            for eslesim in sayfa.search_for(bolum_deseni):
                                bolum_bolgesi = {
                                    "x0": 0,                 # Sol kenar boşluğu
                                    "y0": eslesim.y0,        # Bölüm başlığından başla
                                    "x1": sayfa.rect.width,  # Sağ kenar boşluğu (tam genişlik)
                                    "y1": sayfa.rect.height, # Sayfanın altı
                                    "section": bolum_deseni
                                }
                                
                                haric_tutulan_bolgeler.append(bolum_bolgesi)
                                logging.info(f"Sayfa {sayfa_num+1}'de '{bolum_deseni}' hariç tutulan bölümü bulundu")
                    
                    if haric_tutulan_bolgeler:
                        self.haric_tutulan_sayfa_bolgeleri[sayfa_num] = haric_tutulan_bolgeler
                        logging.info(f"Sayfa {sayfa_num+1}: {len(haric_tutulan_bolgeler)} hariç tutulan bölge bulundu")
        
        except Exception as e:
            logging.error(f"Hariç tutulan bölümleri tanımlama hatası: {str(e)}")