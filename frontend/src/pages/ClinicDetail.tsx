import { Alert, Box, Button, Chip, Link as MuiLink, Paper, Rating, Stack, Typography } from '@mui/material';
import { useQuery } from '@tanstack/react-query';
import { Link, useParams } from 'react-router-dom';
import { api, Clinic, Recommendation, Review } from '../services/api';
import { ErrorState, Loading } from '../components/PageState';

export default function ClinicDetail() {
  const { id = '' } = useParams();
  const provider = useQuery({ queryKey: ['provider', id], queryFn: () => api<Clinic>(`/clinics/${id}`) });
  const reviews = useQuery({ queryKey: ['reviews', id], queryFn: () => api<Review[]>(`/reviews?clinicId=${id}`) });
  const recommendations = useQuery({ queryKey: ['recommendations', id], queryFn: () => api<Recommendation[]>(`/recommendations?clinicId=${id}`) });

  if (provider.isLoading) return <Loading />;
  if (provider.error) return <ErrorState error={provider.error} />;

  return (
    <Stack spacing={3}>
      <Paper sx={{ p: 3 }}>
        <Typography variant="overline">{provider.data!.type.replace(/([A-Z])/g, ' $1').trim()}</Typography>
        <Typography variant="h4">{provider.data!.name}</Typography>
        <Typography color="text.secondary">{provider.data!.specialty} · {provider.data!.city}, {provider.data!.country}</Typography>
        <Typography sx={{ mt: 1 }}>{provider.data!.address}</Typography>
        {provider.data!.publicWebsiteUrl && (
          <MuiLink href={provider.data!.publicWebsiteUrl} target="_blank" rel="noreferrer" sx={{ display: 'block', mt: 1 }}>
            Provider website
          </MuiLink>
        )}
        <Stack direction="row" spacing={2} sx={{ mt: 2 }}>
          <Button component={Link} to={`/review/${id}`} variant="contained">Share your experience</Button>
          <Button component={Link} to={`/recommend?clinicId=${id}`} variant="outlined">Recommend this provider</Button>
        </Stack>
      </Paper>

      <Typography variant="h5">Recommended by patients</Typography>
      {recommendations.isLoading && <Loading />}
      {recommendations.error && <ErrorState error={recommendations.error} />}
      {recommendations.data?.map((rec) => (
        <Paper sx={{ p: 2 }} key={rec.id}>
          <Typography variant="body2" color="text.secondary">
            {rec.authorDisplayName} · {new Date(rec.createdAt).toLocaleDateString()}
          </Typography>
          <Stack direction="row" spacing={1} flexWrap="wrap" sx={{ my: 1 }}>
            {rec.diagnoses.map((diagnosis) => <Chip key={diagnosis} label={diagnosis} size="small" color="primary" variant="outlined" />)}
          </Stack>
          <Typography sx={{ my: 1 }}>{rec.details}</Typography>
        </Paper>
      ))}
      {recommendations.data?.length === 0 && <Alert severity="info">No patient recommendations for this provider yet.</Alert>}

      <Typography variant="h5">People&apos;s experiences</Typography>
      {reviews.isLoading && <Loading />}
      {reviews.error && <ErrorState error={reviews.error} />}
      {reviews.data?.map((review) => (
        <Paper sx={{ p: 2 }} key={review.id}>
          <Stack direction="row" justifyContent="space-between">
            <Typography fontWeight="bold">{review.title}</Typography>
            <Rating value={review.rating} readOnly />
          </Stack>
          <Typography variant="body2" color="text.secondary">{review.authorDisplayName} · {new Date(review.createdAt).toLocaleDateString()}</Typography>
          <Typography sx={{ my: 1 }}>{review.body}</Typography>
          <Stack direction="row" spacing={1}>
            {review.tags.map((tag) => <Chip key={tag} label={tag} size="small" />)}
          </Stack>
        </Paper>
      ))}
      {reviews.data?.length === 0 && <Alert severity="info">No experiences have been published yet.</Alert>}
    </Stack>
  );
}