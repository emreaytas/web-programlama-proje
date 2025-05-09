import io
import os
import sys
import logging
import traceback
from flask import Flask, request, send_file, jsonify

# Yeni anonimleyiciyi içe aktar
from pdf_anonymizer_improved import PDFAnonimleyici

# Loglama konfigürasyonu (değiştirilmedi)
logging.basicConfig(
    level=logging.DEBUG,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s',
    handlers=[
        logging.StreamHandler(sys.stdout)
    ]
)
logger = logging.getLogger("pdf_anonimleyici_servis")

app = Flask(__name__)

# Flask konfigürasyonu (değiştirilmedi)
app.config['MAX_CONTENT_LENGTH'] = 16 * 1024 * 1024  # 16 MB limit
app.config['UPLOAD_FOLDER'] = '/tmp'

@app.route('/health', methods=['GET'])
def saglik_kontrolu():
    """Servis sağlık kontrolü uç noktası"""
    return jsonify({
        "status": "up", 
        "message": "PDF Anonimleştirme servisi çalışıyor",
        "version": "1.2.0",
        "library": "PyMuPDF"
    }), 200

@app.route('/anonymize-pdf', methods=['POST'])
def pdf_anonimize_et():
    """
    Geliştirilmiş kaplama yaklaşımını kullanarak PDF belgesini anonimize et.
    Bu uç nokta orijinal uygulamayı değiştirir ancak aynı arayüzü korur.
    """
    if 'pdf' not in request.files:
        logger.error("İstekte PDF dosyası yok")
        return "PDF dosyası gerekli", 400
    
    pdf_dosyasi = request.files['pdf']
    logger.info(f"PDF alındı: {pdf_dosyasi.filename}, boyut: {pdf_dosyasi.content_length if hasattr(pdf_dosyasi, 'content_length') else 'bilinmiyor'} bayt")
    
    pdf_baytlari = pdf_dosyasi.read()
    pdf_akisi = io.BytesIO(pdf_baytlari)
    
    # İstekten anonimleştirme seçeneklerini ayrıştır
    isimleri_anonimize_et = request.form.get('anonymize_names', 'true').lower() == 'true'
    epostalari_anonimize_et = request.form.get('anonymize_emails', 'true').lower() == 'true'
    kurumlari_anonimize_et = request.form.get('anonymize_institutions', 'true').lower() == 'true'
    
    logger.info(f"Anonimleştirme seçenekleri: isimler={isimleri_anonimize_et}, "
               f"epostalar={epostalari_anonimize_et}, kurumlar={kurumlari_anonimize_et}")
    
    try:
        # Geliştirilmiş anonimleyiciyi kullan
        anonimleyici = PDFAnonimleyici(pdf_akisi)
        logger.info("PDF analiz ediliyor...")
        bilgi = anonimleyici.bilgi_cikar()
        
        logger.info(f"Algılanan: {len(bilgi.get('yazar_isimleri', []))} yazar, "
                  f"{len(bilgi.get('epostalar', []))} eposta, "
                  f"{len(bilgi.get('kurumlar', []))} kurum")
        
        # Bulunanların detaylarını logla
        if bilgi.get('yazar_isimleri'):
            logger.debug(f"Yazarlar: {', '.join(bilgi.get('yazar_isimleri', []))}")
        if bilgi.get('epostalar'):
            logger.debug(f"Epostalar: {', '.join(bilgi.get('epostalar', []))}")
        if bilgi.get('kurumlar'):
            logger.debug(f"Kurumlar: {', '.join(bilgi.get('kurumlar', []))}")
        
        if not bilgi.get('yazar_isimleri') and not bilgi.get('epostalar') and not bilgi.get('kurumlar'):
            logger.warning("PDF'de anonimize edilecek içerik algılanmadı!")
        
        logger.info("PDF anonimize ediliyor...")
        sonuc_akisi = anonimleyici.anonimize_et(
            isimleri_anonimize_et=isimleri_anonimize_et,
            epostalari_anonimize_et=epostalari_anonimize_et,
            kurumlari_anonimize_et=kurumlari_anonimize_et
        )
        
        # İşlenmiş PDF'i döndür
        sonuc_akisi.seek(0)
        sonuc_boyutu = sonuc_akisi.getbuffer().nbytes
        logger.info(f"Başarılı! Anonimize edilmiş PDF boyutu: {sonuc_boyutu} bayt")
        
        # Hata ayıklama için boyutları karşılaştır
        orijinal_boyut = len(pdf_baytlari)
        if sonuc_boyutu == orijinal_boyut:
            logger.warning("Uyarı: Anonimize edilmiş PDF orijinal PDF ile aynı boyutta")
        
        # İçeriğin farklı olduğunu doğrula
        if len(pdf_baytlari) > 100:
            pdf_akisi.seek(0)
            orijinal_ornek = pdf_baytlari[:100]
            sonuc_ornegi = sonuc_akisi.read(100)
            sonuc_akisi.seek(0)
            
            if orijinal_ornek == sonuc_ornegi:
                logger.warning("Uyarı: PDF içeriğinin ilk 100 baytı aynı")
            else:
                logger.info("PDF içeriği başarıyla değiştirildi (ilk 100 bayt farklı)")
        
        return send_file(
            sonuc_akisi,
            mimetype='application/pdf',
            as_attachment=True,
            download_name='anonimize_edilmis.pdf'
        )
    except Exception as e:
        hata_bilgisi = traceback.format_exc()
        logger.error(f"PDF anonimleştirme hatası: {str(e)}")
        logger.error(f"Hata detayları: {hata_bilgisi}")
        return f"PDF anonimleştirme hatası: {str(e)}", 500

# Diğer uç noktaları değiştirmeden koru (extract-info, version, vb.)

if __name__ == '__main__':
    # PyMuPDF'in kullanılabilir olup olmadığını kontrol et
    try:
        import fitz
        logger.info(f"PyMuPDF versiyonu: {fitz.version[0]}")
    except ImportError:
        logger.critical("PyMuPDF (fitz) kütüphanesi bulunamadı! Lütfen şu komutla yükleyin: pip install PyMuPDF")
        sys.exit(1)
        
    port = int(os.environ.get('PORT', 5000))
    app.run(host='0.0.0.0', port=port, debug=True)