import { Container } from '@mui/material';
import { Navigate, Route, Routes } from 'react-router-dom';
import Navbar from './components/Navbar';
import Home from './pages/Home';
import Login from './pages/Login';
import Register from './pages/Register';
import VerifyEmail from './pages/VerifyEmail';
import PatientProfile from './pages/PatientProfile';
import ClinicSearch from './pages/ClinicSearch';
import ClinicDetail from './pages/ClinicDetail';
import WriteReview from './pages/WriteReview';
import ClinicPatientSearch from './pages/ClinicPatientSearch';
import PeopleSearch from './pages/PeopleSearch';
import Matches from './pages/Matches';

export default function App() {
  return (
    <>
      <Navbar />
      <Container maxWidth={false} disableGutters>
        <Routes>
          <Route path="/" element={<Home />} />
          <Route path="/login" element={<Login />} />
          <Route path="/register" element={<Register />} />
          <Route path="/verify-email" element={<VerifyEmail />} />
          <Route path="/clinics" element={<ClinicSearch />} />
          <Route path="/clinics/:id" element={<ClinicDetail />} />
          <Route path="/review/:clinicId" element={<WriteReview />} />
          <Route path="/profile" element={<PatientProfile />} />
          <Route path="/people" element={<PeopleSearch />} />
          <Route path="/matches" element={<Matches />} />
          <Route path="/clinic-patients" element={<ClinicPatientSearch />} />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </Container>
    </>
  );
}
