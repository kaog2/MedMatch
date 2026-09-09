import { SvgIcon, SvgIconProps } from '@mui/material';

/** Heart with medical cross — the MedMatch brand icon. */
export default function MedMatchIcon(props: SvgIconProps) {
  return (
    <SvgIcon {...props} viewBox="0 0 24 24">
      {/* Heart silhouette – subtle backdrop fill */}
      <path
        d="M12 21.35l-1.45-1.32C5.4 15.36 2 12.28 2 8.5 2 5.42 4.42 3 7.5 3c1.74 0 3.41.81 4.5 2.09C13.09 3.81 14.76 3 16.5 3 19.58 3 22 5.42 22 8.5c0 3.78-3.4 6.86-8.55 11.54L12 21.35z"
        opacity={0.25}
      />
      {/* Heart with medical cross cut-out */}
      <path
        fillRule="evenodd"
        clipRule="evenodd"
        d="M12 21.35l-1.45-1.32C5.4 15.36 2 12.28 2 8.5 2 5.42 4.42 3 7.5 3c1.74 0 3.41.81 4.5 2.09C13.09 3.81 14.76 3 16.5 3 19.58 3 22 5.42 22 8.5c0 3.78-3.4 6.86-8.55 11.54L12 21.35zM11 7.5h2v3h3v2h-3v3h-2v-3H8v-2h3v-3z"
      />
    </SvgIcon>
  );
}

