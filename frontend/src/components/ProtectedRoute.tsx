import { Navigate } from 'react-router-dom'
import type { ReactNode } from 'react'
import { useAuth } from '../context/useAuth'

export default function ProtectedRoute({ children }: { children: ReactNode }) {
  const { logado } = useAuth()
  if (!logado) return <Navigate to="/login" replace />
  return <>{children}</>
}
