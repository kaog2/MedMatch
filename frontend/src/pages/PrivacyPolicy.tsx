import { Typography } from '@mui/material';
import LegalPage from './LegalPage';

export default function PrivacyPolicy() {
  return (
    <LegalPage title="Política de Privacidad" lastUpdated="5 de octubre de 2026">
      <Typography variant="body1" sx={{ mb: 3 }}>
        La presente Política de Privacidad describe cómo MedMatch (en adelante, «la Plataforma»
        o «nosotros») recopila, utiliza, almacena y protege los datos personales de sus usuarios,
        en conformidad con la Ley N° 19.628 sobre Protección de la Vida Privada y sus
        modificaciones posteriores vigentes en la República de Chile.
      </Typography>

      {/* ── 1. Responsable del Tratamiento ── */}
      <Typography variant="h5">1. Responsable del Tratamiento</Typography>
      <Typography variant="body1">
        El responsable del tratamiento de los datos personales es MedMatch, plataforma digital de
        intermediación en salud. Para cualquier consulta relacionada con la privacidad y el
        tratamiento de datos personales, el usuario puede contactarnos en:
      </Typography>
      <ul>
        <li><Typography variant="body1"><strong>Correo electrónico:</strong> privacidad@medmatch.app</Typography></li>
        <li><Typography variant="body1"><strong>Sitio web:</strong> https://medmatch.app</Typography></li>
      </ul>

      {/* ── 2. Datos que Recopilamos ── */}
      <Typography variant="h5">2. Datos que Recopilamos</Typography>

      <Typography variant="h6">2.1 Datos de Registro</Typography>
      <Typography variant="body1">
        Al crear una cuenta, recopilamos los siguientes datos:
      </Typography>
      <ul>
        <li><Typography variant="body1">Dirección de correo electrónico</Typography></li>
        <li><Typography variant="body1">Contraseña (almacenada exclusivamente en formato de hash criptográfico irreversible)</Typography></li>
        <li><Typography variant="body1">Tipo de cuenta seleccionado (Paciente, Clínica, Médico)</Typography></li>
        <li><Typography variant="body1">Fecha y hora de registro</Typography></li>
      </ul>

      <Typography variant="h6">2.2 Datos Sensibles de Salud</Typography>
      <Typography variant="body1">
        Conforme al artículo 2°, letra g) de la Ley N° 19.628, los datos referentes al estado de
        salud física o psíquica de las personas constituyen <strong>«datos sensibles»</strong>.
        MedMatch recopila las siguientes categorías de datos sensibles cuando el usuario
        voluntariamente los proporciona:
      </Typography>
      <ul>
        <li><Typography variant="body1">Diagnósticos médicos y condiciones de salud declaradas</Typography></li>
        <li><Typography variant="body1">Síntomas físicos y psicológicos reportados</Typography></li>
        <li><Typography variant="body1">Intervenciones médicas y tratamientos recibidos</Typography></li>
        <li><Typography variant="body1">Registros del diario de dolor y síntomas: localización corporal, tipos de dolor
          (punzante, ardiente, pulsátil, sordo, irradiado, etc.), niveles de severidad (escala 0 a 10),
          duración, factores desencadenantes y aliviantes</Typography></li>
        <li><Typography variant="body1">Medicamentos tomados</Typography></li>
        <li><Typography variant="body1">Indicadores de calidad de sueño y bienestar general</Typography></li>
        <li><Typography variant="body1">Notas textuales de reflexión sobre el estado de salud diario</Typography></li>
      </ul>

      {/* ── 3. Consentimiento Explícito ── */}
      <Typography variant="h5">3. Consentimiento Explícito para Datos Sensibles</Typography>
      <Typography variant="body1">
        De conformidad con lo establecido en el artículo 4°, inciso tercero, de la Ley N° 19.628,
        el tratamiento de datos sensibles requiere el <strong>consentimiento previo, expreso e
        informado</strong> del titular.
      </Typography>
      <Typography variant="body1" sx={{ mt: 1.5 }}>
        Al registrarse en MedMatch, completar su perfil de salud y/o utilizar el diario de síntomas,
        el usuario otorga libre y voluntariamente su consentimiento expreso para el tratamiento
        de sus datos sensibles de salud, exclusivamente para las finalidades descritas en la
        sección 4 de esta política.
      </Typography>
      <Typography variant="body1" sx={{ mt: 1.5 }}>
        <strong>Este consentimiento puede ser revocado en cualquier momento</strong>, sin expresión
        de causa, mediante solicitud escrita a privacidad@medmatch.app o eliminando directamente
        la cuenta desde la configuración de la plataforma. La revocación del consentimiento
        no afectará la licitud del tratamiento realizado con anterioridad.
      </Typography>

      {/* ── 4. Finalidad del Tratamiento ── */}
      <Typography variant="h5">4. Finalidad del Tratamiento</Typography>
      <Typography variant="body1">
        Los datos personales y datos sensibles de salud son tratados <strong>única y
        exclusivamente</strong> para las siguientes finalidades:
      </Typography>
      <ul>
        <li><Typography variant="body1"><strong>Matching de pacientes:</strong> Vincular usuarios con condiciones de salud similares para
          que puedan conectarse y compartir experiencias de manera voluntaria.</Typography></li>
        <li><Typography variant="body1"><strong>Recomendaciones personalizadas:</strong> Sugerir centros médicos, clínicas y
          especialistas relevantes en función de los diagnósticos declarados por el usuario.</Typography></li>
        <li><Typography variant="body1"><strong>Analítica personal:</strong> Generar tendencias y estadísticas agregadas y
          anonimizadas destinadas exclusivamente al propio usuario dentro de su diario de síntomas.</Typography></li>
        <li><Typography variant="body1"><strong>Registro vía chatbot:</strong> Permitir, de forma opcional, el registro de síntomas a
          través de integraciones con bots de mensajería (n8n) utilizando claves API personales.</Typography></li>
      </ul>
      <Typography variant="body1" sx={{ fontWeight: 700, mt: 1 }}>
        MedMatch NO vende, NO comparte con anunciantes y NO utiliza los datos de salud del
        usuario para perfilamiento comercial o publicitario bajo ninguna circunstancia.
      </Typography>

      {/* ── 5. Derechos ARCO ── */}
      <Typography variant="h5">5. Derechos ARCO del Titular</Typography>
      <Typography variant="body1">
        De conformidad con el artículo 12 y siguientes de la Ley N° 19.628, todo usuario tiene
        derecho a ejercer en cualquier momento los siguientes derechos sobre sus datos personales:
      </Typography>
      <ul>
        <li><Typography variant="body1"><strong>Acceso:</strong> Solicitar una copia completa de todos los datos personales
          almacenados en la plataforma.</Typography></li>
        <li><Typography variant="body1"><strong>Rectificación:</strong> Corregir datos personales que sean inexactos,
          incompletos o desactualizados.</Typography></li>
        <li><Typography variant="body1"><strong>Cancelación:</strong> Solicitar la eliminación definitiva de sus datos personales
          (derecho al olvido). MedMatch procederá a la eliminación dentro de los 30 días corridos
          siguientes a la solicitud.</Typography></li>
        <li><Typography variant="body1"><strong>Oposición:</strong> Oponerse a determinadas actividades de tratamiento de sus datos,
          incluido el matching con otros usuarios o las recomendaciones de centros médicos.</Typography></li>
      </ul>
      <Typography variant="body1">
        <strong>¿Cómo ejercer estos derechos?</strong>
      </Typography>
      <ul>
        <li><Typography variant="body1">Enviando un correo electrónico a <strong>privacidad@medmatch.app</strong> con el asunto
          «Solicitud ARCO», indicando su nombre, correo asociado a la cuenta y el derecho
          que desea ejercer.</Typography></li>
        <li><Typography variant="body1">MedMatch responderá a toda solicitud dentro de un plazo máximo de <strong>2 días
          hábiles</strong>.</Typography></li>
        <li><Typography variant="body1">Alternativamente, el usuario puede eliminar su cuenta directamente desde la
          configuración de su perfil en la plataforma.</Typography></li>
      </ul>

      {/* ── 6. Almacenamiento y Seguridad ── */}
      <Typography variant="h5">6. Almacenamiento y Seguridad</Typography>
      <Typography variant="body1">
        MedMatch implementa medidas técnicas y organizativas apropiadas para proteger los datos
        personales contra acceso no autorizado, alteración, divulgación o destrucción:
      </Typography>
      <ul>
        <li><Typography variant="body1">Cifrado de datos en tránsito mediante TLS (Transport Layer Security).</Typography></li>
        <li><Typography variant="body1">Cifrado de datos en reposo en la base de datos.</Typography></li>
        <li><Typography variant="body1">Contraseñas almacenadas con funciones de hash criptográfico irreversible.</Typography></li>
        <li><Typography variant="body1">Claves API de bots almacenadas exclusivamente como hashes SHA-256.</Typography></li>
        <li><Typography variant="body1">Infraestructura alojada en servidores europeos con certificaciones de seguridad.</Typography></li>
      </ul>
      <Typography variant="body1">
        <strong>Retención de datos:</strong> Los datos personales se conservan únicamente mientras la
        cuenta del usuario permanezca activa. Tras una solicitud de eliminación de cuenta, todos
        los datos serán eliminados de forma definitiva dentro de los 30 días corridos siguientes.
      </Typography>

      {/* ── 7. Cookies ── */}
      <Typography variant="h5">7. Cookies y Tecnologías de Seguimiento</Typography>
      <Typography variant="body1">
        MedMatch utiliza <strong>exclusivamente cookies esenciales de sesión</strong> (tokens JWT)
        necesarias para el funcionamiento de la autenticación del usuario. La plataforma:
      </Typography>
      <ul>
        <li><Typography variant="body1">NO utiliza cookies publicitarias ni de terceros.</Typography></li>
        <li><Typography variant="body1">NO emplea rastreadores de terceros (Google Analytics, Facebook Pixel, etc.).</Typography></li>
        <li><Typography variant="body1">Utiliza OpenTelemetry exclusivamente para monitoreo de rendimiento y errores de
          la aplicación, sin exportar datos personales de salud.</Typography></li>
      </ul>

      {/* ── 8. Transferencia Internacional ── */}
      <Typography variant="h5">8. Transferencia Internacional de Datos</Typography>
      <Typography variant="body1">
        En caso de que los datos personales sean transferidos fuera del territorio de la República
        de Chile, MedMatch garantiza que dicha transferencia se realizará cumpliendo con lo
        dispuesto en el artículo 5° de la Ley N° 19.628, asegurando que el país de destino
        proporcione niveles adecuados de protección de datos personales.
      </Typography>

      {/* ── 9. Modificaciones ── */}
      <Typography variant="h5">9. Modificaciones a esta Política</Typography>
      <Typography variant="body1">
        MedMatch se reserva el derecho de actualizar la presente Política de Privacidad en
        cualquier momento. En caso de cambios sustanciales, se notificará a los usuarios mediante:
      </Typography>
      <ul>
        <li><Typography variant="body1">Correo electrónico dirigido a la dirección asociada a su cuenta.</Typography></li>
        <li><Typography variant="body1">Notificación visible dentro de la plataforma.</Typography></li>
      </ul>
      <Typography variant="body1">
        El uso continuado de la plataforma tras la notificación de los cambios constituirá
        aceptación de la política modificada.
      </Typography>

      {/* ── 10. Contacto ── */}
      <Typography variant="h5">10. Contacto</Typography>
      <Typography variant="body1">
        Para cualquier consulta, solicitud o reclamo relacionado con la privacidad de sus datos:
      </Typography>
      <ul>
        <li><Typography variant="body1"><strong>Correo electrónico:</strong> privacidad@medmatch.app</Typography></li>
      </ul>
    </LegalPage>
  );
}

