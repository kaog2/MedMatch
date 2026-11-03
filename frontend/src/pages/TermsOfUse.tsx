import { Box, Typography } from '@mui/material';
import LegalPage from './LegalPage';

export default function TermsOfUse() {
  return (
    <LegalPage title="Términos y Condiciones de Uso" lastUpdated="5 de octubre de 2026">
      {/* ── 1. Aceptación ── */}
      <Typography variant="h5">1. Aceptación de los Términos</Typography>
      <Typography variant="body1">
        Al acceder, navegar o utilizar la plataforma MedMatch (en adelante, «la Plataforma»,
        «el Servicio» o «nosotros»), el usuario acepta íntegramente los presentes Términos y
        Condiciones de Uso. Si no está de acuerdo con alguna de sus disposiciones, debe
        abstenerse de utilizar la Plataforma.
      </Typography>
      <Typography variant="body1" sx={{ mt: 1.5 }}>
        El uso continuado de la Plataforma tras la publicación de modificaciones a estos términos
        constituye aceptación tácita de las mismas.
      </Typography>

      {/* ── 2. Descripción del Servicio ── */}
      <Typography variant="h5">2. Descripción del Servicio</Typography>
      <Typography variant="body1">
        MedMatch es una plataforma digital que permite a sus usuarios:
      </Typography>
      <ul>
        <li><Typography variant="body1">Registrar perfiles de salud describiendo dolencias físicas, dolores y síntomas.</Typography></li>
        <li><Typography variant="body1">Vincular («matching») usuarios con condiciones de salud similares para la conexión
          entre pares y el intercambio voluntario de experiencias.</Typography></li>
        <li><Typography variant="body1">Recibir recomendaciones informativas de centros médicos, clínicas y especialistas
          basadas en los diagnósticos declarados.</Typography></li>
        <li><Typography variant="body1">Llevar un diario diario de dolor y síntomas con análisis de tendencias personales.</Typography></li>
        <li><Typography variant="body1">Opcionalmente, registrar síntomas mediante bots de mensajería integrados a través
          de n8n (Telegram, WhatsApp, Slack, etc.).</Typography></li>
      </ul>

      {/* ── 3. EXENCIÓN DE RESPONSABILIDAD MÉDICA ── */}
      <Typography variant="h5">3. Exención de Responsabilidad Médica</Typography>
      <Box
        sx={{
          bgcolor: 'rgba(176,111,66,.08)',
          border: '2px solid #b06f42',
          borderRadius: 2,
          p: 3,
          my: 2,
        }}
      >
        <Typography variant="body1" sx={{ fontWeight: 800, color: '#b06f42', fontSize: '1.05rem', mb: 1.5 }}>
          ⚠ AVISO IMPORTANTE — LÉASE CUIDADOSAMENTE
        </Typography>
        <Typography variant="body1" sx={{ fontWeight: 700 }}>
          MedMatch NO es un servicio médico. MedMatch NO provee diagnósticos clínicos.
          MedMatch NO prescribe ni recomienda tratamientos médicos.
        </Typography>
        <Typography variant="body1" sx={{ mt: 1.5 }}>
          Las conexiones entre usuarios son intercambios informativos entre pares y
          <strong> NO constituyen consejo médico, diagnóstico ni opinión profesional</strong>.
        </Typography>
        <Typography variant="body1" sx={{ mt: 1.5 }}>
          Las recomendaciones de clínicas, centros médicos y especialistas que aparecen en la
          Plataforma son <strong>referencias informativas únicamente</strong>. MedMatch NO
          garantiza la calidad, seguridad, eficacia ni resultados de ningún tratamiento o
          servicio proporcionado por dichos proveedores.
        </Typography>
        <Typography variant="body1" sx={{ mt: 1.5, fontWeight: 700 }}>
          El usuario DEBE consultar a profesionales de la salud calificados antes de tomar
          cualquier decisión médica. MedMatch NO asume responsabilidad alguna por los
          resultados de salud derivados de la información intercambiada en la Plataforma.
        </Typography>
      </Box>

      {/* ── 4. Limitación de Responsabilidad sobre Recomendaciones ── */}
      <Typography variant="h5">4. Limitación de Responsabilidad sobre Recomendaciones</Typography>
      <Typography variant="body1">
        MedMatch actúa exclusivamente como <strong>herramienta intermediaria de
        comunicación</strong> y referencia informativa. En consecuencia:
      </Typography>
      <ul>
        <li><Typography variant="body1">Toda relación contractual, médica o comercial que surja a raíz de una recomendación
          de la Plataforma es <strong>estricta y exclusivamente entre el usuario y el centro
          médico o profesional de salud correspondiente</strong>.</Typography></li>
        <li><Typography variant="body1">MedMatch <strong>no verifica</strong> las credenciales, habilitaciones profesionales, registros
          sanitarios ni la calidad de atención de los proveedores listados en la Plataforma.</Typography></li>
        <li><Typography variant="body1">MedMatch <strong>no es responsable</strong> de la veracidad, exactitud, integridad ni
          actualización de la información proporcionada por proveedores de salud terceros.</Typography></li>
        <li><Typography variant="body1">MedMatch <strong>no asume responsabilidad</strong> por daños directos, indirectos, incidentales,
          consecuenciales o especiales derivados de las recomendaciones de la Plataforma.</Typography></li>
      </ul>

      {/* ── 5. Obligaciones del Usuario ── */}
      <Typography variant="h5">5. Obligaciones del Usuario</Typography>
      <Typography variant="body1">
        Al utilizar la Plataforma, el usuario se compromete a:
      </Typography>
      <ul>
        <li><Typography variant="body1"><strong>Proporcionar información veraz:</strong> Los datos ingresados en el perfil de salud y
          el diario de síntomas deben ser auténticos y corresponder a la situación real del usuario.</Typography></li>
        <li><Typography variant="body1"><strong>Uso lícito:</strong> No utilizar la Plataforma con fines ilícitos, fraudulentos o que
          perjudiquen a otros usuarios o a terceros.</Typography></li>
        <li><Typography variant="body1"><strong>Confidencialidad de credenciales:</strong> Mantener la seguridad de su contraseña
          y claves API. MedMatch no será responsable de accesos no autorizados derivados
          de la negligencia del usuario en la custodia de sus credenciales.</Typography></li>
        <li><Typography variant="body1"><strong>Respeto a otros usuarios:</strong> Respetar la privacidad, dignidad y derechos de
          los demás usuarios de la Plataforma.</Typography></li>
        <li><Typography variant="body1"><strong>No divulgación de información de terceros:</strong> No compartir información de
          salud de otros usuarios fuera de la Plataforma sin su consentimiento expreso.</Typography></li>
      </ul>

      {/* ── 6. Propiedad Intelectual ── */}
      <Typography variant="h5">6. Propiedad Intelectual</Typography>
      <Typography variant="body1">
        Todo el código fuente, diseño, marca, logotipos, estructura de la interfaz de usuario
        y contenido agregado anonimizado generado por la Plataforma son propiedad exclusiva
        de MedMatch y están protegidos por la legislación chilena e internacional sobre
        propiedad intelectual.
      </Typography>
      <Typography variant="body1" sx={{ mt: 1.5 }}>
        Los usuarios conservan la propiedad de sus datos personales de salud y pueden ejercer
        sus derechos ARCO en cualquier momento conforme a lo descrito en la Política de Privacidad.
      </Typography>

      {/* ── 7. Suspensión y Terminación ── */}
      <Typography variant="h5">7. Suspensión y Terminación de Cuentas</Typography>
      <Typography variant="body1">
        MedMatch se reserva el derecho de suspender temporal o definitivamente, o terminar,
        la cuenta de cualquier usuario que:
      </Typography>
      <ul>
        <li><Typography variant="body1">Incumpla los presentes Términos y Condiciones.</Typography></li>
        <li><Typography variant="body1">Proporcione información falsa o engañosa.</Typography></li>
        <li><Typography variant="body1">Utilice la Plataforma de manera abusiva, fraudulenta o contraria a la ley.</Typography></li>
        <li><Typography variant="body1">Comprometa la seguridad, integridad o funcionamiento de la Plataforma.</Typography></li>
        <li><Typography variant="body1">Acose, difame o perjudique a otros usuarios.</Typography></li>
      </ul>
      <Typography variant="body1">
        La suspensión o terminación será comunicada al usuario con indicación de los motivos.
        En caso de terminación definitiva, el usuario podrá ejercer sus derechos ARCO respecto
        de sus datos personales.
      </Typography>

      {/* ── 8. Ley Aplicable y Jurisdicción ── */}
      <Typography variant="h5">8. Ley Aplicable y Jurisdicción</Typography>
      <Typography variant="body1">
        Los presentes Términos y Condiciones se rigen e interpretan conforme a la legislación
        vigente de la <strong>República de Chile</strong>.
      </Typography>
      <Typography variant="body1" sx={{ mt: 1.5 }}>
        Para la resolución de cualquier controversia derivada del uso de la Plataforma, las
        partes se someten a la jurisdicción de los <strong>Tribunales Ordinarios de Justicia
        con sede en Santiago de Chile</strong>, renunciando expresamente a cualquier otro fuero
        que pudiere corresponderles.
      </Typography>

      {/* ── 9. Modificaciones ── */}
      <Typography variant="h5">9. Modificaciones</Typography>
      <Typography variant="body1">
        MedMatch se reserva el derecho de modificar los presentes Términos y Condiciones en
        cualquier momento. Los cambios sustanciales serán notificados a los usuarios mediante
        correo electrónico y/o aviso dentro de la Plataforma con al menos 15 días de anticipación
        a su entrada en vigencia.
      </Typography>

      {/* ── 10. Contacto ── */}
      <Typography variant="h5">10. Contacto</Typography>
      <Typography variant="body1">
        Para consultas relacionadas con estos Términos y Condiciones:
      </Typography>
      <ul>
        <li><Typography variant="body1"><strong>Correo electrónico:</strong> legal@medmatch.app</Typography></li>
      </ul>
    </LegalPage>
  );
}

