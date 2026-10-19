import { useQuery } from "@tanstack/react-query";
import type { ColumnDef } from "@tanstack/react-table";
import { useCallback, useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import { BgtActionMenu } from "@/components/BgtActionMenu/BgtActionMenu";
import BgtButton from "@/components/BgtButton/BgtButton";
import {
	BgtDialog,
	BgtDialogContent,
	BgtDialogDescription,
	BgtDialogFooter,
	BgtDialogTitle,
} from "@/components/BgtDialog";
import { BgtFieldLabel, BgtInputField, BgtSelect } from "@/components/BgtForm";
import { BgtLoadingSpinner } from "@/components/BgtLoadingSpinner/BgtLoadingSpinner";
import { BgtDataTable } from "@/components/BgtTable/BgtDataTable";
import { useAppForm } from "@/hooks/form";
import { useAuth } from "@/hooks/useAuth";
import { useModalState } from "@/hooks/useModalState";
import type {
	BgtSelectItem,
	PlayerLink,
	ProfileResponse,
	ResetPasswordResponse,
	UpdateProfileRequest,
	UserDto,
} from "@/models";
import { useToasts } from "@/routes/-hooks/useToasts";
import { BgtDeleteModal } from "@/routes/-modals/BgtDeleteModal";
import { getSettings } from "@/services/queries/settings";
import { toDisplayDateTime } from "@/utils/dateUtils";
import { handleFormSubmit } from "@/utils/formUtils";
import { useAccountData } from "../-hooks/useAccountData";
import { ChangePasswordModal } from "../-modals/ChangePasswordModal";
import { CreateUserModal } from "../-modals/CreateUserModal";
import { EditUserModal } from "../-modals/EditUserModal";
import { TempPasswordModal } from "../-modals/TempPasswordModal";
import { UserAccessModal } from "../-modals/UserAccessModal";
import { ExternalLoginsSection } from "./ExternalLoginsSection";
import { SettingsSection } from "./SettingsSection";

export const AccountSettings = () => {
	const { t } = useTranslation(["settings", "auth", "common"]);
	const currentUser = useAuth((s) => s.user);
	const { errorToast } = useToasts();

	const {
		profile,
		isProfileLoading,
		users,
		linkablePlayers,
		players,
		isAdmin,
		updateProfile,
		isUpdatingProfile,
		changePassword,
		isChangingPassword,
		registerUser,
		isRegistering,
		deleteUser,
		updateUser,
		isUpdatingUser,
		resetPassword,
	} = useAccountData();

	const changePasswordModal = useModalState();
	const createUserModal = useModalState();
	const [deleteTarget, setDeleteTarget] = useState<UserDto | null>(null);
	const [editTarget, setEditTarget] = useState<UserDto | null>(null);
	const [accessTargetId, setAccessTargetId] = useState<string | null>(null);
	const [resetTarget, setResetTarget] = useState<UserDto | null>(null);
	const [isResetting, setIsResetting] = useState(false);
	const accessTarget = users.find((user) => user.id === accessTargetId) ?? null;
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
				linkablePlayers={linkablePlayers}
				updateProfile={updateProfile}
				isUpdatingProfile={isUpdatingProfile}
				onChangePassword={changePasswordModal.show}
			/>

			<ExternalLoginsSection />

			{isAdmin && (
				<UserManagementSection
					users={users}
					currentUserId={currentUser?.id ?? ""}
					onCreateUser={createUserModal.show}
					onEditUser={(user) => setEditTarget(user)}
					onManageAccess={(user) => setAccessTargetId(user.id)}
					onResetPassword={(user) => setResetTarget(user)}
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
					players={players}
					users={users}
					onSubmit={registerUser}
					isLoading={isRegistering}
				/>
			)}

			{editTarget && (
				<EditUserModal
					open={true}
					close={() => setEditTarget(null)}
					user={editTarget}
					players={players}
					users={users}
					onSubmit={updateUser}
					isLoading={isUpdatingUser}
				/>
			)}

			{accessTarget && (
				<UserAccessModal
					open={true}
					close={() => setAccessTargetId(null)}
					user={accessTarget}
					isCurrentUser={accessTarget.id === currentUser?.id}
				/>
			)}

			{resetTarget && (
				<BgtDialog open onClose={() => setResetTarget(null)}>
					<BgtDialogContent>
						<BgtDialogTitle>{t("account.users.reset-password.title", { user: resetTarget.username })}</BgtDialogTitle>
						<BgtDialogDescription>{t("account.users.reset-password.description")}</BgtDialogDescription>
						<BgtDialogFooter
							onCancel={() => setResetTarget(null)}
							confirmLabel={t("account.users.reset-password.button")}
							variant="destructive"
							loading={isResetting}
							onConfirm={async () => {
								setIsResetting(true);
								try {
									const result: ResetPasswordResponse = await resetPassword(resetTarget.id);
									setTempPasswordData({ username: resetTarget.username, password: result.tempPassword });
								} catch {
									errorToast("settings:account.notifications.password-reset-failed");
								} finally {
									setIsResetting(false);
									setResetTarget(null);
								}
							}}
						/>
					</BgtDialogContent>
				</BgtDialog>
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
					description={t("account.users.delete.description", { username: deleteTarget.username })}
				/>
			)}
		</>
	);
};

const emailChanged = (email: string, current: string | null) =>
	email.trim() !== "" && email.trim().toLowerCase() !== (current ?? "").trim().toLowerCase();

interface ProfileSectionProps {
	profile: ProfileResponse;
	linkablePlayers: PlayerLink[];
	updateProfile: (req: UpdateProfileRequest) => Promise<unknown>;
	isUpdatingProfile: boolean;
	onChangePassword: () => void;
}

const ProfileSection = ({
	profile,
	linkablePlayers,
	updateProfile,
	isUpdatingProfile,
	onChangePassword,
}: ProfileSectionProps) => {
	const { t } = useTranslation(["settings", "auth", "common"]);

	const playerItems: BgtSelectItem[] = useMemo(
		() => [
			{ value: 0, label: t("account.profile.player.none") },
			...linkablePlayers.map((p) => ({ value: p.id, label: p.name })),
		],
		[linkablePlayers, t],
	);

	const form = useAppForm({
		defaultValues: {
			displayName: profile.displayName ?? "",
			email: profile.email ?? "",
			playerId: profile.playerId ?? 0,
			currentPassword: "",
		},
		onSubmit: async ({ value }) => {
			await updateProfile({
				displayName: value.displayName || null,
				email: value.email || null,
				playerId: value.playerId ? value.playerId : null,
				currentPassword: emailChanged(value.email, profile.email) ? value.currentPassword : null,
			});
		},
	});

	return (
		<SettingsSection title={t("account.profile.title")}>
			<form onSubmit={handleFormSubmit(form)} className="space-y-3">
				<div>
					<BgtFieldLabel>{t("account.profile.username.label")}</BgtFieldLabel>
					<div className="text-[15px] text-white/70">{profile.username}</div>
				</div>
				<form.Field name="displayName">
					{(field) => (
						<BgtInputField
							field={field}
							type="text"
							label={t("account.profile.display-name.label")}
							disabled={isUpdatingProfile}
						/>
					)}
				</form.Field>
				<form.Field name="email">
					{(field) => (
						<BgtInputField
							field={field}
							type="text"
							label={t("account.profile.email.label")}
							disabled={isUpdatingProfile}
						/>
					)}
				</form.Field>
				<form.Subscribe selector={(state) => state.values.email}>
					{(email) =>
						emailChanged(email, profile.email) && (
							<form.Field name="currentPassword">
								{(field) => (
									<BgtInputField
										field={field}
										type="password"
										label={t("account.profile.current-password.label")}
										placeholder={t("account.profile.current-password.placeholder")}
										disabled={isUpdatingProfile}
									/>
								)}
							</form.Field>
						)
					}
				</form.Subscribe>
				<form.Field name="playerId">
					{(field) => (
						<BgtSelect
							field={field}
							label={t("account.profile.player.label")}
							items={playerItems}
							hasSearch
							disabled={isUpdatingProfile}
						/>
					)}
				</form.Field>
				<div className="flex gap-3 pt-2">
					<BgtButton type="submit" disabled={isUpdatingProfile}>
						{t("account.profile.save")}
					</BgtButton>
					<BgtButton variant="cancel" onClick={onChangePassword}>
						{t("account.password.change-button")}
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
	onEditUser: (user: UserDto) => void;
	onManageAccess: (user: UserDto) => void;
	onResetPassword: (user: UserDto) => void;
	onDeleteUser: (user: UserDto) => void;
}

const UserManagementSection = ({
	users,
	currentUserId,
	onCreateUser,
	onEditUser,
	onManageAccess,
	onResetPassword,
	onDeleteUser,
}: UserManagementSectionProps) => {
	const { t } = useTranslation(["settings", "auth", "common"]);
	const { data: settings } = useQuery(getSettings());
	const dateFormat = settings?.dateFormat ?? "yyyy-MM-dd";
	const timeFormat = settings?.timeFormat ?? "HH:mm";
	const uiLanguage = settings?.uiLanguage ?? "en-US";

	const lastLogin = useCallback(
		(user: UserDto) =>
			user.lastLoginAt
				? toDisplayDateTime(user.lastLoginAt, dateFormat, timeFormat, uiLanguage)
				: t("account.users.never-logged-in"),
		[t, dateFormat, timeFormat, uiLanguage],
	);

	const roleNames = useCallback(
		(user: UserDto) => user.roles.map((role) => t(`account.users.roles.${role.toLowerCase()}`)).join(", "),
		[t],
	);

	const badges = useCallback(
		(user: UserDto) => (
			<>
				{user.isDisabled && (
					<span className="rounded-sm bg-error-strong/30 px-1.5 py-0.5 text-[11px] text-white">
						{t("account.users.access.disabled-badge")}
					</span>
				)}
				{user.externalLoginCount > 0 && (
					<span className="rounded-sm bg-primary/30 px-1.5 py-0.5 text-[11px] text-white">
						{t("account.users.access.sso-badge")}
					</span>
				)}
			</>
		),
		[t],
	);

	const columns: ColumnDef<UserDto>[] = useMemo(
		() => [
			{
				accessorKey: "username",
				header: t("auth:username"),
				cell: ({ row }) => (
					<div className="flex flex-wrap items-center gap-2">
						<span>{row.original.username}</span>
						{badges(row.original)}
					</div>
				),
			},
			{
				accessorKey: "roles",
				header: t("account.users.role"),
				cell: ({ row }) => <div className="text-white/70">{roleNames(row.original)}</div>,
			},
			{
				accessorKey: "lastLoginAt",
				header: t("account.users.last-login"),
				cell: ({ row }) => <div className="text-white/70">{lastLogin(row.original)}</div>,
			},
			{
				id: "actions",
				header: "",
				cell: ({ row }) => (
					<div className="flex justify-end gap-2">
						<BgtButton size="1" variant="cancel" onClick={() => onEditUser(row.original)}>
							{t("common:edit")}
						</BgtButton>
						<BgtButton size="1" variant="cancel" onClick={() => onManageAccess(row.original)}>
							{t("account.users.access.button")}
						</BgtButton>
						<BgtButton size="1" variant="cancel" onClick={() => onResetPassword(row.original)}>
							{t("account.users.reset-password.button")}
						</BgtButton>
						{row.original.id !== currentUserId && (
							<BgtButton size="1" variant="error" onClick={() => onDeleteUser(row.original)}>
								{t("common:delete.button")}
							</BgtButton>
						)}
					</div>
				),
			},
		],
		[t, currentUserId, onEditUser, onManageAccess, onResetPassword, onDeleteUser, lastLogin, badges, roleNames],
	);

	return (
		<SettingsSection title={t("account.users.title")}>
			<div className="mb-4">
				<BgtButton onClick={onCreateUser}>{t("account.users.create-button")}</BgtButton>
			</div>
			<div className="hidden md:block">
				<BgtDataTable columns={columns} data={users} noDataMessage={t("account.users.no-users")} />
			</div>
			<ul className="flex flex-col gap-2 md:hidden" aria-label={t("account.users.title")}>
				{users.length === 0 && <li className="text-sm text-white/60">{t("account.users.no-users")}</li>}
				{users.map((user) => (
					<li
						key={user.id}
						data-testid="user-card"
						className="flex items-start gap-3 rounded-lg border border-white/10 bg-white/5 p-3"
					>
						<div className="flex min-w-0 flex-1 flex-col gap-1">
							<div className="flex flex-wrap items-center gap-2">
								<span className="truncate font-semibold text-white">{user.username}</span>
								{badges(user)}
							</div>
							<span className="text-sm text-white/70">{roleNames(user)}</span>
							<span className="text-xs text-white/50">
								{t("account.users.last-login")}: {lastLogin(user)}
							</span>
						</div>
						<BgtActionMenu
							label={t("account.users.actions", { username: user.username })}
							items={[
								{ label: t("common:edit"), onSelect: () => onEditUser(user) },
								{ label: t("account.users.access.button"), onSelect: () => onManageAccess(user) },
								{ label: t("account.users.reset-password.button"), onSelect: () => onResetPassword(user) },
								...(user.id !== currentUserId
									? [{ label: t("common:delete.button"), onSelect: () => onDeleteUser(user), destructive: true }]
									: []),
							]}
						/>
					</li>
				))}
			</ul>
		</SettingsSection>
	);
};
