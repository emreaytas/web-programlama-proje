import io
import re
import logging
from typing import Dict, List, Set, Tuple, Optional, Union
import PyPDF2
from PyPDF2 import PdfReader, PdfWriter
from PyPDF2.generic import NameObject, createStringObject

# Logger oluşturma
logger = logging.getLogger("pdf_anonymizer")

class PDFAnonymizer:
    """
    PDF dosyalarını anonimleştiren sınıf.
    Yazar adları, iletişim bilgileri ve kurum bilgilerini tespit eder ve anonimleştirir.
    """
    
    # Anonimleştirme yapılmayacak bölüm başlıkları
    EXCLUDED_SECTIONS = [
        r'giriş',
        r'ilgili\s*çalışmalar',
        r'referanslar',
        r'kaynakça',
        r'kaynaklar',
        r'teşekkür'
    ]
    
    def __init__(self, pdf_stream: io.BytesIO):
        """
        PDF anonimleştirici nesnesini oluşturur.
        
        Args:
            pdf_stream: PDF içeriğini barındıran BytesIO nesnesi
        """
        self.pdf_stream = pdf_stream
        
        # PDF okuma işlemi
        try:
            pdf_stream.seek(0)  # Stream'i başa sar
            self.reader = PdfReader(pdf_stream)
            logger.info(f"PDF başarıyla açıldı, {len(self.reader.pages)} sayfa içeriyor")
        except Exception as e:
            logger.error(f"PDF açılırken hata: {str(e)}")
            raise
            
        self.writer = PdfWriter()
        
        # Tespit edilen bilgileri saklar
        self.author_names = set()
        self.emails = set()
        self.institutions = set()
        
        # Anonimleştirme yapılmayacak bölgeler
        self.excluded_regions = []
        
        # Anonimleştirilmiş içeriği saklar
        self.anonymized_text = {}
        
    def extract_information(self) -> Dict:
        """
        PDF'den yazar isimleri, e-posta adresleri ve kurumlar gibi
        anonimleştirilecek bilgileri çıkarır.
        
        Returns:
            Dict: Tespit edilen bilgileri içeren sözlük
        """
        logger.info("PDF meta bilgileri ve içeriği çıkarılıyor...")
        
        # PDF metadata'sından bilgi çıkar
        metadata = {}
        try:
            if hasattr(self.reader, 'metadata') and self.reader.metadata:
                metadata = {
                    'author': getattr(self.reader.metadata, 'author', None),
                    'creator': getattr(self.reader.metadata, 'creator', None),
                    'producer': getattr(self.reader.metadata, 'producer', None),
                    'title': getattr(self.reader.metadata, 'title', None),
                    'subject': getattr(self.reader.metadata, 'subject', None)
                }
                logger.debug(f"PDF meta bilgileri: {metadata}")
        except Exception as e:
            logger.warning(f"Meta bilgileri çıkarılırken hata: {str(e)}")
            # Meta bilgileri çıkaramazsak boş devam et
            
        # Her sayfadan metin çıkar
        all_text = ""
        page_count = len(self.reader.pages)
        logger.info(f"PDF'den metin çıkarılıyor ({page_count} sayfa)")
        
        for i in range(page_count):
            try:
                page = self.reader.pages[i]
                text = page.extract_text()
                if text:
                    all_text += text + "\n"
                    logger.debug(f"Sayfa {i+1}: {len(text)} karakter çıkarıldı")
                else:
                    logger.warning(f"Sayfa {i+1}: Metin çıkarılamadı veya boş")
            except Exception as e:
                logger.error(f"Sayfa {i+1} metin çıkarma hatası: {str(e)}")
                # Hata olursa bu sayfayı atla
        
        # Yazar isimlerini tespit et
        author_names = self._find_author_names(all_text)
        
        # E-posta adreslerini tespit et
        emails = self._find_emails(all_text)
        
        # Kurum isimlerini tespit et
        institutions = self._find_institutions(all_text)
        
        # Anonimleştirilmeyecek bölümleri belirle
        excluded_regions = self._identify_excluded_sections(all_text)
        
        # Bulguları sakla
        self.author_names = author_names
        self.emails = emails
        self.institutions = institutions
        self.excluded_regions = excluded_regions
        
        # Sonuçları döndür
        return {
            'metadata': metadata,
            'author_names': list(author_names),
            'emails': list(emails),
            'institutions': list(institutions),
            'excluded_sections': [{'start': start, 'end': end, 'title': title} 
                                for start, end, title in excluded_regions]
        }
    
    def anonymize(self, 
                 anonymize_names: bool = True, 
                 anonymize_emails: bool = True,
                 anonymize_institutions: bool = True) -> io.BytesIO:
        """
        PDF'i anonimleştirir. Yazar isimleri, e-posta adresleri ve kurum bilgilerini
        yıldız veya referans kodları ile değiştirir.
        
        Args:
            anonymize_names: Yazar isimlerini anonimleştir
            anonymize_emails: E-posta adreslerini anonimleştir
            anonymize_institutions: Kurum bilgilerini anonimleştir
            
        Returns:
            io.BytesIO: Anonimleştirilmiş PDF içeriği
        """
        logger.info("PDF anonimleştirme işlemi başlatılıyor...")
        
        # İlk olarak bilgileri çıkar
        if not self.author_names and not self.emails and not self.institutions:
            logger.info("İlk olarak PDF içeriğinden bilgiler çıkarılıyor...")
            self.extract_information()
            
        # Anonimleştirme referans kodları
        name_replacements = {}
        email_replacements = {}
        institution_replacements = {}
        
        # Yazar isimleri için referans kodları oluştur
        if anonymize_names:
            for i, name in enumerate(self.author_names):
                name_replacements[name] = f"[AUTHOR_{i+1}]"
            logger.info(f"{len(name_replacements)} yazar ismi için referans kodu oluşturuldu")
                
        # E-posta adresleri için referans kodları oluştur
        if anonymize_emails:
            for i, email in enumerate(self.emails):
                email_replacements[email] = f"[EMAIL_{i+1}]"
            logger.info(f"{len(email_replacements)} e-posta adresi için referans kodu oluşturuldu")
                
        # Kurum isimleri için referans kodları oluştur
        if anonymize_institutions:
            for i, institution in enumerate(self.institutions):
                institution_replacements[institution] = f"[INSTITUTION_{i+1}]"
            logger.info(f"{len(institution_replacements)} kurum bilgisi için referans kodu oluşturuldu")
        
        # Metadata düzenle
        if anonymize_names and hasattr(self.reader, 'metadata') and self.reader.metadata:
            logger.info("PDF metadata anonimleştiriliyor...")
            try:
                # Yeni metadata hazırla
                metadata = {}
                if hasattr(self.reader.metadata, 'author') and self.reader.metadata.author:
                    metadata['/Author'] = '[ANONYMOUS AUTHOR]'
                
                # Diğer metadata alanlarını kopyala
                if hasattr(self.reader.metadata, 'creator') and self.reader.metadata.creator:
                    metadata['/Creator'] = self.reader.metadata.creator
                if hasattr(self.reader.metadata, 'producer') and self.reader.metadata.producer:
                    metadata['/Producer'] = self.reader.metadata.producer
                if hasattr(self.reader.metadata, 'title') and self.reader.metadata.title:
                    metadata['/Title'] = self.reader.metadata.title
                if hasattr(self.reader.metadata, 'subject') and self.reader.metadata.subject:
                    metadata['/Subject'] = self.reader.metadata.subject
                
                # Metadata ayarla
                self.writer.add_metadata(metadata)
                logger.debug(f"Anonimleştirilmiş metadata: {metadata}")
            except Exception as e:
                logger.error(f"Metadata anonimleştirme hatası: {str(e)}")
        
        # Her sayfayı işle ve içerik düzenle
        logger.info("Sayfalar işleniyor ve anonimleştiriliyor...")
        page_count = len(self.reader.pages)
        
        for i in range(page_count):
            try:
                # Orijinal sayfayı al
                original_page = self.reader.pages[i]
                page_text = original_page.extract_text()
                
                # Sayfadaki metni değiştir
                modified_text = self._apply_anonymization(
                    page_text, 
                    name_replacements if anonymize_names else {}, 
                    email_replacements if anonymize_emails else {}, 
                    institution_replacements if anonymize_institutions else {}
                )
                
                # Sayfa içeriğini düzenle
                new_page = self._replace_text_in_page(original_page, page_text, modified_text)
                
                # Düzenlenmiş sayfayı ekle
                self.writer.add_page(new_page)
                
                # Sayfanın anonimleştirilmiş metnini sakla
                self.anonymized_text[i] = modified_text
                
                logger.debug(f"Sayfa {i+1}/{page_count} işlendi")
            except Exception as e:
                logger.error(f"Sayfa {i+1} işlenirken hata: {str(e)}")
                # Hata olsa bile diğer sayfalara devam et
                # Hataya rağmen sayfayı kopyalamaya çalış
                try:
                    self.writer.add_page(original_page)
                except Exception as inner_e:
                    logger.error(f"Sayfa {i+1} kopyalanamadı: {str(inner_e)}")
        
        # Anonimleştirilmiş PDF'i belleğe yaz
        logger.info("Anonimleştirilmiş PDF oluşturuluyor...")
        output_stream = io.BytesIO()
        
        try:
            self.writer.write(output_stream)
            output_size = output_stream.getbuffer().nbytes
            logger.info(f"Anonimleştirilmiş PDF oluşturuldu, boyut: {output_size} byte")
            output_stream.seek(0)
            return output_stream
        except Exception as e:
            logger.error(f"PDF yazma hatası: {str(e)}")
            # Hata durumunda orijinal PDF'i döndürmeyi deneyelim
            self.pdf_stream.seek(0)
            logger.warning("Hata nedeniyle orijinal PDF iade ediliyor")
            return self.pdf_stream
    
    def _replace_text_in_page(self, page, original_text, modified_text):
        """
        Sayfadaki metni değiştirir. Bu metot her sayfayı yeniden oluşturmak yerine
        mevcut sayfanın kopyasını alıp içeriğini değiştirir.
        
        Bu metot PyPDF2'nin sınırlamaları nedeniyle mükemmel çalışmayabilir.
        
        Args:
            page: Orijinal sayfa
            original_text: Orijinal metin
            modified_text: Değiştirilmiş metin
            
        Returns:
            Değiştirilmiş sayfa
        """
        # Buradaki değişiklikler PyPDF2'nin sınırlamaları nedeniyle tam olarak çalışmayabilir
        # Ancak bazı durumlarda işe yarayabilir
        
        # Sayfa kopyasını al
        page_copy = page
        
        try:
            # Sayfanın içerik akışını al
            if '/Contents' in page:
                content_object = page['/Contents']
                
                # İçerik bir diziyse (birden fazla akış)
                if isinstance(content_object, list):
                    for i in range(len(content_object)):
                        stream_object = content_object[i]
                        stream_data = stream_object.get_data()
                        
                        # İçerikte her değiştirilecek metni ara ve değiştir
                        for old_text, new_text in self._get_text_replacements(original_text, modified_text):
                            if old_text in stream_data.decode('latin-1', errors='ignore'):
                                stream_data = stream_data.replace(
                                    old_text.encode('latin-1', errors='ignore'),
                                    new_text.encode('latin-1', errors='ignore')
                                )
                        
                        # Değiştirilmiş içeriği geri yaz
                        content_object[i] = PyPDF2.generic.StreamObject.initialize_from_data(stream_data)
                
                # İçerik tek bir akışsa
                else:
                    stream_data = content_object.get_data()
                    
                    # İçerikte her değiştirilecek metni ara ve değiştir
                    for old_text, new_text in self._get_text_replacements(original_text, modified_text):
                        if old_text in stream_data.decode('latin-1', errors='ignore'):
                            stream_data = stream_data.replace(
                                old_text.encode('latin-1', errors='ignore'),
                                new_text.encode('latin-1', errors='ignore')
                            )
                    
                    # Değiştirilmiş içeriği geri yaz
                    page['/Contents'] = PyPDF2.generic.StreamObject.initialize_from_data(stream_data)
        except Exception as e:
            logger.error(f"Sayfa içeriği değiştirilirken hata: {str(e)}")
            # Hata durumunda orijinal sayfayı döndür
        
        return page_copy
    
    def _get_text_replacements(self, original_text, modified_text):
        """
        Orijinal ve değiştirilmiş metin arasındaki farklılıkları bulur ve
        değiştirilecek metinleri döndürür.
        
        Args:
            original_text: Orijinal metin
            modified_text: Değiştirilmiş metin
            
        Returns:
            List[Tuple[str, str]]: Değiştirilecek metin çiftleri
        """
        replacements = []
        
        # Yazar isimlerini bul ve değiştir
        for name in self.author_names:
            if name in original_text and f"[AUTHOR_" in modified_text:
                author_id = next((i+1 for i, n in enumerate(self.author_names) if n == name), 0)
                if author_id > 0:
                    replacements.append((name, f"[AUTHOR_{author_id}]"))
        
        # E-posta adreslerini bul ve değiştir
        for email in self.emails:
            if email in original_text and f"[EMAIL_" in modified_text:
                email_id = next((i+1 for i, e in enumerate(self.emails) if e == email), 0)
                if email_id > 0:
                    replacements.append((email, f"[EMAIL_{email_id}]"))
        
        # Kurum isimlerini bul ve değiştir
        for institution in self.institutions:
            if institution in original_text and f"[INSTITUTION_" in modified_text:
                inst_id = next((i+1 for i, inst in enumerate(self.institutions) if inst == institution), 0)
                if inst_id > 0:
                    replacements.append((institution, f"[INSTITUTION_{inst_id}]"))
        
        return replacements
    
    def _apply_anonymization(self, 
                            text: str, 
                            name_replacements: Dict[str, str], 
                            email_replacements: Dict[str, str],
                            institution_replacements: Dict[str, str]) -> str:
        """
        Metin üzerinde anonimleştirme değişikliklerini uygular.
        Anonimleştirilmeyecek bölgeleri hariç tutar.
        
        Args:
            text: Orijinal metin
            name_replacements: İsim -> Referans kodu eşleşmeleri
            email_replacements: E-posta -> Referans kodu eşleşmeleri
            institution_replacements: Kurum -> Referans kodu eşleşmeleri
            
        Returns:
            str: Anonimleştirilmiş metin
        """
        # Anonimleştirilecek bölgeleri belirle
        text_regions = [(0, len(text))]  # Başlangıçta tüm metin
        
        # Hariç tutulan bölgeleri çıkar
        for start, end, _ in self.excluded_regions:
            new_regions = []
            for region_start, region_end in text_regions:
                # Bölge tamamen hariç tutulan bir bölgenin içindeyse, atla
                if region_start >= start and region_end <= end:
                    continue
                
                # Bölge hariç tutulan bölgenin öncesindeyse, aynen ekle
                if region_end <= start:
                    new_regions.append((region_start, region_end))
                    continue
                
                # Bölge hariç tutulan bölgenin sonrasındaysa, aynen ekle
                if region_start >= end:
                    new_regions.append((region_start, region_end))
                    continue
                
                # Bölge, hariç tutulan bölgeyle kesişiyorsa, ayır
                if region_start < start:
                    new_regions.append((region_start, start))
                
                if region_end > end:
                    new_regions.append((end, region_end))
            
            text_regions = new_regions
        
        # Anonimleştirme işlemini uygula
        result_text = list(text)  # Karakterleri liste olarak değiştireceğiz
        
        changes_made = 0  # Takip için değişiklik sayısı
        
        # Yazar isimlerini değiştir
        for name, replacement in name_replacements.items():
            for match in re.finditer(re.escape(name), text):
                start, end = match.span()
                
                # Bu eşleşme anonimleştirilecek bir bölgede mi kontrol et
                for region_start, region_end in text_regions:
                    if start >= region_start and end <= region_end:
                        # Değişikliği uygula
                        result_text[start:end] = list(replacement) + [''] * (end - start - len(replacement))
                        changes_made += 1
                        logger.debug(f"Yazar ismi değiştirildi: '{name}' -> '{replacement}'")
                        break
        
        # E-posta adreslerini değiştir
        for email, replacement in email_replacements.items():
            for match in re.finditer(re.escape(email), text):
                start, end = match.span()
                
                # Bu eşleşme anonimleştirilecek bir bölgede mi kontrol et
                for region_start, region_end in text_regions:
                    if start >= region_start and end <= region_end:
                        # Değişikliği uygula
                        result_text[start:end] = list(replacement) + [''] * (end - start - len(replacement))
                        changes_made += 1
                        logger.debug(f"E-posta adresi değiştirildi: '{email}' -> '{replacement}'")
                        break
        
        # Kurum isimlerini değiştir
        for institution, replacement in institution_replacements.items():
            for match in re.finditer(re.escape(institution), text):
                start, end = match.span()
                
                # Bu eşleşme anonimleştirilecek bir bölgede mi kontrol et
                for region_start, region_end in text_regions:
                    if start >= region_start and end <= region_end:
                        # Değişikliği uygula
                        result_text[start:end] = list(replacement) + [''] * (end - start - len(replacement))
                        changes_made += 1
                        logger.debug(f"Kurum bilgisi değiştirildi: '{institution}' -> '{replacement}'")
                        break
        
        logger.info(f"Toplam {changes_made} anonimleştirme değişikliği yapıldı")
        
        # Sonucu string'e çevir
        return ''.join(c for c in result_text if c)
        
    def _find_author_names(self, text: str) -> Set[str]:
        """
        Metinden muhtemel yazar isimlerini çıkarır.
        
        Args:
            text: PDF metni
            
        Returns:
            Set[str]: Tespit edilen yazar isimleri kümesi
        """
        # Yazar isimlerini bulmak için bazı yaygın desenler
        patterns = [
            # Yazar bölümü / başlıkları
            r'(?:Author|Yazar|Authors)s?:?\s*([\w\s,\.]+)', 
            r'(?:İsim|Ad|Soyad)[\s:]+([\w\s]+)',
            
            # Makale yazarları - yaygın formatlar
            r'([A-Z][a-z]+\s+[A-Z][a-z]+)(?:\s*,\s*\d)?',
            r'([A-Z][a-z]+\s+[A-Z]\.)\s',
            
            # İsim ve soyisim formatları
            r'([A-Z][a-z]+\s+[A-Z][a-z\-]+)',  # John Smith, Ali Yılmaz
            r'([A-Z][a-z]+\s+[A-Z]\.\s+[A-Z][a-z]+)', # John A. Smith
            
            # Akademik makalelerde yaygın yazar formatları
            r'([A-Z][a-z]+\s+et\s+al\.)',  # Smith et al.
            r'([A-Z][a-z]+\s+and\s+[A-Z][a-z]+)', # Smith and Johnson
        ]
        
        authors = set()
        for pattern in patterns:
            matches = re.finditer(pattern, text)
            for match in matches:
                author = match.group(1).strip()
                # İsim olabilecek bir şeyse ekle (en az bir boşluk içermeli)
                if ' ' in author and len(author) > 4:
                    authors.add(author)
                    logger.debug(f"Yazar ismi tespit edildi: '{author}'")
        
        return authors
    
    def _find_emails(self, text: str) -> Set[str]:
        """
        Metinden e-posta adreslerini çıkarır.
        
        Args:
            text: PDF metni
            
        Returns:
            Set[str]: Tespit edilen e-posta adresleri kümesi
        """
        # E-posta adresi deseni
        # Daha kapsamlı bir regex deseni kullanıyoruz
        email_pattern = r'[\w\.-]+@[\w\.-]+\.\w+(?:\.\w+)?'
        
        emails = set()
        matches = re.finditer(email_pattern, text)
        for match in matches:
            email = match.group(0).strip()
            emails.add(email)
            logger.debug(f"E-posta adresi tespit edildi: '{email}'")
            
        return emails
    
    def _find_institutions(self, text: str) -> Set[str]:
        """
        Metinden kurum isimlerini çıkarır.
        
        Args:
            text: PDF metni
            
        Returns:
            Set[str]: Tespit edilen kurum isimleri kümesi
        """
        # Türk üniversitelerinin tipik isimleri ve diğer akademik kurumlar
        uni_patterns = [
            r'(\w+\s+Üniversitesi)',
            r'(\w+\s+University)',
            r'(İstanbul\s+\w+\s+Üniversitesi)',
            r'(Kocaeli\s+\w+\s+Üniversitesi)',
            r'(University\s+of\s+\w+)',
            r'(\w+\s+Institute\s+of\s+Technology)',
            r'(\w+\s+College)',
        ]
        
        # Diğer kurum türleri
        other_patterns = [
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
        
        institutions = set()
        
        # Tüm desenleri dene
        for pattern in uni_patterns + other_patterns:
            matches = re.finditer(pattern, text)
            for match in matches:
                institution = match.group(1).strip()
                if len(institution) > 5:  # Çok kısa kurum isimlerini filtrele
                    institutions.add(institution)
                    logger.debug(f"Kurum ismi tespit edildi: '{institution}'")
        
        return institutions
    
    def _identify_excluded_sections(self, text: str) -> List[Tuple[int, int, str]]:
        """
        Anonimleştirilmeyecek bölümleri belirler (Giriş, Referanslar, vb.)
        
        Args:
            text: PDF metni
            
        Returns:
            List[Tuple[int, int, str]]: Bölüm başlangıç/bitiş indisleri ve başlıkları
        """
        excluded_regions = []
        
        # Tüm muhtemel bölüm başlıklarını ara
        for pattern in self.EXCLUDED_SECTIONS:
            # Hem büyük harfli hem de küçük harfli versiyonlarını kontrol et
            for pat in [pattern, pattern.upper()]:
                # Başlık sonrası genellikle nokta, boşluk veya satır sonu gelir
                full_pattern = rf'({pat})[\.:\s]'
                matches = re.finditer(full_pattern, text, re.IGNORECASE)
                
                for match in matches:
                    section_start = match.start()
                    section_title = match.group(1)
                    
                    # Bölüm sonunu bul (sonraki bölüm başlangıcı veya dosya sonu)
                    section_end = len(text)
                    
                    # Diğer bölüm başlıklarını ara ve ilk bulunanı bölüm sonu olarak kabul et
                    for end_pattern in self.EXCLUDED_SECTIONS:
                        if end_pattern != pattern:  # Aynı başlığı tekrar arama
                            end_match = re.search(rf'({end_pattern})[\.:\s]', text[section_start + len(section_title):], re.IGNORECASE)
                            if end_match:
                                next_section_start = section_start + len(section_title) + end_match.start()
                                if next_section_start < section_end:
                                    section_end = next_section_start
                    
                    excluded_regions.append((section_start, section_end, section_title))
                    logger.debug(f"Anonimleştirilmeyecek bölüm tespit edildi: '{section_title}' ({section_start}-{section_end})")
        
        return excluded_regions
    
    
    