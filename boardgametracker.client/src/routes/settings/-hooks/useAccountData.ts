import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useAuth } from "@/hooks/useAuth";
import { QUERY_KEYS } from "@/models";
import { useToasts } from "@/routes/-hooks/useToasts";
import {
	changePasswordCall,
	deleteUserCall,
	registerUserCall,
	resetPasswordCall,
	updateProfileCall,
	updateUserCall,
} from "@/services/authService";
import { getLinkablePlayers, getProfile, getUsers } from "@/services/queries/auth";
import { getPlayers } from "@/services/queries/players";

export const useAccountData = () => {
	const queryClient = useQueryClient();
	const { successToast, errorToast } = useToasts();
	const isAdmin = useAuth((s) => s.hasRole("Admin"));

	const profileQuery = useQuery(getProfile());
	const usersQuery = useQuery({ ...getUsers(), enabled: isAdmin });
	const linkablePlayersQuery = useQuery(getLinkablePlayers());
	const playersQuery = useQuery({ ...getPlayers(), enabled: isAdmin });

	const updateProfileMutation = useMutation({
		mutationFn: updateProfileCall,
		onSuccess: () => {
			successToast("settings:account.notifications.profile-updated");
			queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.profile] });
			queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.players] });
		},
		onError: () => {
			errorToast("settings:account.notifications.profile-update-failed");
		},
	});

	const changePasswordMutation = useMutation({
		mutationFn: changePasswordCall,
		onSuccess: () => {
			successToast("settings:account.notifications.password-changed");
		},
		onError: () => {
			errorToast("settings:account.notifications.password-change-failed");
		},
	});

	const registerUserMutation = useMutation({
		mutationFn: registerUserCall,
		onSuccess: () => {
			successToast("settings:account.notifications.user-created");
			queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.users] });
			queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.players] });
		},
		onError: () => {
			errorToast("settings:account.notifications.user-create-failed");
		},
	});

	const deleteUserMutation = useMutation({
		mutationFn: deleteUserCall,
		onSuccess: () => {
			successToast("settings:account.notifications.user-deleted");
			queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.users] });
		},
		onError: () => {
			errorToast("settings:account.notifications.user-delete-failed");
		},
	});

	const updateUserMutation = useMutation({
		mutationFn: ({
			userId,
			...request
		}: {
			userId: string;
			username: string;
			email: string | null;
			role: string;
			playerId: number | null;
		}) => updateUserCall(userId, request),
		onSuccess: () => {
			successToast("settings:account.notifications.user-updated");
			queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.users] });
			queryClient.invalidateQueries({ queryKey: [QUERY_KEYS.players] });
		},
		onError: () => {
			errorToast("settings:account.notifications.user-update-failed");
		},
	});

	return {
		profile: profileQuery.data,
		isProfileLoading: profileQuery.isLoading,
		users: usersQuery.data ?? [],
		linkablePlayers: linkablePlayersQuery.data ?? [],
		players: playersQuery.data ?? [],
		isAdmin,
		updateProfile: updateProfileMutation.mutateAsync,
		isUpdatingProfile: updateProfileMutation.isPending,
		changePassword: changePasswordMutation.mutateAsync,
		isChangingPassword: changePasswordMutation.isPending,
		registerUser: registerUserMutation.mutateAsync,
		isRegistering: registerUserMutation.isPending,
		deleteUser: deleteUserMutation.mutateAsync,
		updateUser: updateUserMutation.mutateAsync,
		isUpdatingUser: updateUserMutation.isPending,
		resetPassword: resetPasswordCall,
	};
};
