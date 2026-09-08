export interface UserProfile {
  id: number;
  email: string;
  displayName: string;
}

export async function getProfile(id: number): Promise<UserProfile> {
  const response = await fetch(`/api/users/${id}`);
  return response.json();
}
