import { Typography } from '@mui/material';

export default function LegalNoticeCl() {
  return (
    <>
      <Typography variant="body1" sx={{ mb: 3 }}>
        El presente Aviso Legal regula las condiciones generales de acceso y uso del sitio web y la
        plataforma digital MedMatch (en adelante, «la Plataforma»), de conformidad con la
        legislación vigente en la República de Chile.
      </Typography>

      {/* ── 1. Identificación del Titular ── */}
      <Typography variant="h5">1. Identificación del Titular</Typography>
      <Typography variant="body1">
        MedMatch es una plataforma web de intermediación en salud que facilita la conexión entre
        personas con dolencias o síntomas similares, y ofrece recomendaciones de centros médicos,
        clínicas y especialistas.
      </Typography>
      <ul>
        <li>
          <Typography variant="body1">
            <strong>Denominación:</strong> MedMatch
          </Typography>
        </li>
        <li>
          <Typography variant="body1">
            <strong>Sitio web:</strong> https://medmatch.app
          </Typography>
        </li>
        <li>
          <Typography variant="body1">
            <strong>Correo electrónico de contacto:</strong> legal@medmatch.app
          </Typography>
        </li>
      </ul>

      {/* ── 2. Naturaleza del Servicio ── */}
      <Typography variant="h5">2. Naturaleza del Servicio</Typography>
      <Typography variant="body1">
        MedMatch actúa exclusivamente como una <strong>herramienta digital de comunicación e
        intermediación</strong>. La Plataforma:
      </Typography>
      <ul>
        <li>
          <Typography variant="body1">
            <strong>No es un prestador de servicios de salud.</strong> No realiza diagnósticos,
            prescripciones, tratamientos ni ninguna actividad que constituya el ejercicio de la
            medicina.
          </Typography>
        </li>
        <li>
          <Typography variant="body1">
            <strong>No sustituye la consulta médica profesional.</strong> Las conexiones entre
            usuarios y las recomendaciones de centros no reemplazan, bajo ninguna circunstancia,
            la opinión de un profesional de la salud debidamente habilitado.
          </Typography>
        </li>
        <li>
          <Typography variant="body1">
            Facilita la interacción entre personas con experiencias de salud similares y sugiere
            centros de atención basándose en la información proporcionada por los usuarios.
          </Typography>
        </li>
      </ul>

      {/* ── 3. Exoneración de Responsabilidad ── */}
      <Typography variant="h5">3. Exoneración de Responsabilidad</Typography>
      <Typography variant="body1">
        MedMatch declara que:
      </Typography>
      <ul>
        <li>
          <Typography variant="body1">
            <strong>Contenido publicado por usuarios:</strong> Todo contenido, opinión, experiencia
            o dato compartido por los usuarios en la Plataforma es de su exclusiva responsabilidad.
            MedMatch no verifica, valida ni respalda la veracidad, exactitud o integridad de dicho
            contenido.
          </Typography>
        </li>
        <li>
          <Typography variant="body1">
            <strong>Recomendaciones de centros:</strong> Las sugerencias de centros médicos,
            clínicas o especialistas se generan con base en criterios algorítmicos y datos
            disponibles. Dichas recomendaciones <strong>no constituyen un aval, garantía ni
            certificación</strong> de la calidad, competencia o resultados de los servicios
            prestados por dichos centros.
          </Typography>
        </li>
        <li>
          <Typography variant="body1">
            <strong>Disponibilidad del servicio:</strong> MedMatch no garantiza la disponibilidad
            continua e ininterrumpida de la Plataforma. Podrán realizarse labores de mantenimiento,
            actualizaciones o existir interrupciones por causas técnicas o de fuerza mayor, sin que
            ello genere derecho a indemnización alguna.
          </Typography>
        </li>
        <li>
          <Typography variant="body1">
            <strong>Pérdida de datos:</strong> Aunque se implementan medidas de seguridad
            razonables, MedMatch no se responsabiliza por la pérdida, alteración o acceso no
            autorizado a datos derivados de fallos técnicos, ataques informáticos o circunstancias
            fuera de su control directo.
          </Typography>
        </li>
      </ul>

      {/* ── 4. Propiedad Intelectual ── */}
      <Typography variant="h5">4. Propiedad Intelectual</Typography>
      <Typography variant="body1">
        Todos los derechos de propiedad intelectual e industrial sobre la Plataforma, incluyendo
        —de forma enunciativa y no limitativa— el nombre «MedMatch», logotipos, diseño gráfico,
        interfaz de usuario, código fuente, algoritmos, textos originales, estructura de navegación
        y bases de datos, son titularidad exclusiva de MedMatch o de sus licenciantes.
      </Typography>
      <Typography variant="body1" sx={{ mt: 1.5 }}>
        Queda estrictamente prohibida la reproducción, distribución, transformación, comunicación
        pública o cualquier otra forma de explotación, total o parcial, de los contenidos de la
        Plataforma sin la autorización previa y por escrito de MedMatch.
      </Typography>

      {/* ── 5. Enlaces a Terceros ── */}
      <Typography variant="h5">5. Enlaces a Terceros</Typography>
      <Typography variant="body1">
        La Plataforma puede contener enlaces o referencias a sitios web de terceros (centros
        médicos, clínicas, especialistas u otros servicios). MedMatch no controla ni se hace
        responsable de los contenidos o prácticas de dichos sitios web.
      </Typography>

      {/* ── 6. Legislación Aplicable y Jurisdicción ── */}
      <Typography variant="h5">6. Legislación Aplicable y Jurisdicción</Typography>
      <Typography variant="body1">
        El presente Aviso Legal se rige por la legislación vigente en la República de Chile. Para
        cualquier controversia, las partes se someten a la jurisdicción de los <strong>Tribunales
        Ordinarios de Justicia de Santiago de Chile</strong>.
      </Typography>

      {/* ── 7. Contacto ── */}
      <Typography variant="h5">7. Contacto</Typography>
      <ul>
        <li>
          <Typography variant="body1">
            <strong>Correo electrónico:</strong> legal@medmatch.app
          </Typography>
        </li>
      </ul>
    </>
  );
}
