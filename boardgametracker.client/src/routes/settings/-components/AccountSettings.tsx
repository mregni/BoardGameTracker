import { useForm } from "@tanstack/react-form";
import type { ColumnDef } from "@tanstack/react-table";
import { useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import BgtButton from "@/components/BgtButton/BgtButton";
import { BgtInputField } from "@/components/BgtForm";
import { BgtLoadingSpinner } from "@/components/BgtLoadingSpinner/BgtLoadingSpinner";
import { BgtDataTable } from "@/components/BgtTable/BgtDataTable";
import { useAuth } from "@/hooks/useAuth";
import { useModalState } from "@/hooks/useModalState";
import type { ResetPasswordResponse, UserDto } from "@/models";
import { useToasts } from "@/routes/-hooks/useToasts";
import { BgtDeleteModal } from "@/routes/-modals/BgtDeleteModal";
import { handleFormSubmit } from "@/utils/formUtils";
import { useAccountData } from "../-hooks/useAccountData";
import { ChangePasswordModal } from "../-modals/ChangePasswordModal";
import { CreateUserModal } from "../-modals/CreateUserModal";
import { TempPasswordModal } from "../-modals/TempPasswordModal";
import { SettingsSection } from "./SettingsSection";

export const AccountSettings = () => {
	const { t } = useTranslation();
	const currentUser = useAuth((s) => s.user);
	const { errorToast } = useToasts();

	const {
		profile,
		isProfileLoading,
		users,
		isAdmin,
		updateProfile,
		isUpdatingProfile,
		changePassword,
		isChangingPassword,
		registerUser,
		isRegistering,
		deleteUser,
		resetPassword,
	} = useAccountData();

	const changePasswordModal = useModalState();
	const createUserModal = useModalState();
	const [deleteTarget, setDeleteTarget] = useState<UserDto | null>(null);
	const [tempPasswordData, setTempPasswordData] = useState<{ username: string; password: string } | null>(null);

	if (isProfileLoading || !profile) {
		return (
			<div className="flex items-center justify-center py-8">
				<BgtLoadingSpinner />
			</div>
		);
	}

	return (
		<>
			<ProfileSection
				profile={profile}
				updateProfile={updateProfile}
				isUpdatingProfile={isUpdatingProfile}
				onChangePassword={changePasswordModal.show}
			/>

			{isAdmin && (
				<UserManagementSection
					users={users}
					currentUserId={currentUser?.id ?? ""}
					onCreateUser={createUserModal.show}
					onResetPassword={async (user) => {
						try {
							const result: ResetPasswordResponse = await resetPassword(user.id);
							setTempPasswordData({ username: user.username, password: result.tempPassword });
						} catch {
							errorToast("settings.account.notifications.password-reset-failed");
						}
					}}
					onDeleteUser={(user) => setDeleteTarget(user)}
				/>
			)}

			{changePasswordModal.isOpen && (
				<ChangePasswordModal
					open={changePasswordModal.isOpen}
					close={changePasswordModal.hide}
					onSubmit={changePassword}
					isLoading={isChangingPassword}
				/>
			)}

			{createUserModal.isOpen && (
				<CreateUserModal
					open={createUserModal.isOpen}
					close={createUserModal.hide}
					onSubmit={registerUser}
					isLoading={isRegistering}
				/>
			)}

			{tempPasswordData && (
				<TempPasswordModal
					open={true}
					close={() => setTempPasswordData(null)}
					username={tempPasswordData.username}
					tempPassword={tempPasswordData.password}
				/>
			)}

			{deleteTarget && (
				<BgtDeleteModal
					open={true}
					close={() => setDeleteTarget(null)}
					onDelete={async () => {
						await deleteUser(deleteTarget.id);
						setDeleteTarget(null);
					}}
					title={deleteTarget.username}
					description={t("settings.account.users.delete.description", { username: deleteTarget.username })}
				/>
			)}
		</>
	);
};

interface ProfileSectionProps {
	profile: { username: string; email: string | null };
	updateProfile: (req: { username: string; email: string | null }) => Promise<unknown>;
	isUpdatingProfile: boolean;
	onChangePassword: () => void;
}

const ProfileSection = ({ profile, updateProfile, isUpdatingProfile, onChangePassword }: ProfileSectionProps) => {
	const { t } = useTranslation();

	const form = useForm({
		defaultValues: {
			username: profile.username ?? "",
			email: profile.email ?? "",
		},
		onSubmit: async ({ value }) => {
			await updateProfile({
				username: value.username || "",
				email: value.email || null,
			});
		},
	});

	return (
		<SettingsSection
			title={t("settings.account.profile.title")}
			description={t("settings.account.profile.description")}
		>
			<div className="mb-3 text-sm text-white/60">
				<span className="font-medium text-white/80">{profile.username}</span>
			</div>
			<form onSubmit={handleFormSubmit(form)} className="space-y-3">
				<form.Field name="username">
					{(field) => (
						<BgtInputField
							field={field}
							type="text"
							label={t("settings.account.profile.username.label")}
							disabled={isUpdatingProfile}
						/>
					)}
				</form.Field>
				<form.Field name="email">
					{(field) => (
						<BgtInputField
							field={field}
							type="text"
							label={t("settings.account.profile.email.label")}
							disabled={isUpdatingProfile}
						/>
					)}
				</form.Field>
				<div className="flex gap-3 pt-2">
					<BgtButton type="submit" disabled={isUpdatingProfile}>
						{t("settings.account.profile.save")}
					</BgtButton>
					<BgtButton variant="cancel" onClick={onChangePassword}>
						{t("settings.account.password.change-button")}
					</BgtButton>
				</div>
			</form>
		</SettingsSection>
	);
};

interface UserManagementSectionProps {
	users: UserDto[];
	currentUserId: string;
	onCreateUser: () => void;
	onResetPassword: (user: UserDto) => void;
	onDeleteUser: (user: UserDto) => void;
}

const UserManagementSection = ({
	users,
	currentUserId,
	onCreateUser,
	onResetPassword,
	onDeleteUser,
}: UserManagementSectionProps) => {
	const { t } = useTranslation();

	const columns: ColumnDef<UserDto>[] = useMemo(
		() => [
			{
				accessorKey: "username",
				header: t("auth.username"),
				cell: ({ row }) => <div>{row.original.username}</div>,
			},
			{
				accessorKey: "roles",
				header: () => <div className="hidden md:block">{t("settings.account.users.role")}</div>,
				cell: ({ row }) => <div className="text-white/70 hidden md:block">{row.original.roles.join(", ")}</div>,
			},
			{
				accessorKey: "lastLoginAt",
				header: () => <div className="hidden md:block">{t("settings.account.users.last-login")}</div>,
				cell: ({ row }) => (
					<div className="text-white/70 hidden md:block">
						{row.original.lastLoginAt
							? new Date(row.original.lastLoginAt).toLocaleDateString()
							: t("settings.account.users.never-logged-in")}
					</div>
				),
			},
			{
				id: "actions",
				header: "",
				cell: ({ row }) => (
					<div className="flex justify-end gap-2">
						<BgtButton size="1" variant="cancel" onClick={() => onResetPassword(row.original)}>
							{t("settings.account.users.reset-password.button")}
						</BgtButton>
						{row.original.id !== currentUserId && (
							<BgtButton size="1" variant="error" onClick={() => onDeleteUser(row.original)}>
								{t("common.delete.button")}
							</BgtButton>
						)}
					</div>
				),
			},
		],
		[t, currentUserId, onResetPassword, onDeleteUser],
	);

	return (
		<SettingsSection title={t("settings.account.users.title")} description={t("settings.account.users.description")}>
			<div className="mb-4">
				<BgtButton onClick={onCreateUser}>{t("settings.account.users.create-button")}</BgtButton>
			</div>
			<BgtDataTable columns={columns} data={users} noDataMessage={t("settings.account.users.no-users")} />
		</SettingsSection>
	);
};
