import { useState } from 'react';
import { getProfile, UserProfile } from '../api/userApi';

export default function ProfilePage() {
  const [profile, setProfile] = useState<UserProfile | null>(null);

  async function load() {
    setProfile(await getProfile(1));
  }

  return (
    <div>
      <h1>Profile</h1>
      <button onClick={load}>Load</button>
      <p>{profile?.displayName}</p>
    </div>
  );
}
