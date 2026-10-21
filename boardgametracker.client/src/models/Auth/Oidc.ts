import { z } from "zod";

export const OIDC_DEFAULT_ROLES = ["User", "Reader"] as const;

export type OidcDefaultRole = (typeof OIDC_DEFAULT_ROLES)[number];

export interface OidcProviderSummary {
	id: number;
	displayName: string;
	enabled: boolean;
}

export interface OidcProviderConfig extends OidcProviderSummary {
	authority: string;
	clientId: string;
	hasClientSecret: boolean;
	scopes: string;
	autoProvisionUsers: boolean;
	defaultRole: OidcDefaultRole;
	authorizationEndpoint: string | null;
	tokenEndpoint: string | null;
	userInfoEndpoint: string | null;
	usernameClaimType: string | null;
	emailClaimType: string | null;
	displayNameClaimType: string | null;
	pictureClaimType: string | null;
	rolesClaimType: string | null;
	adminGroupValue: string | null;
	iconUrl: string | null;
	buttonColor: string | null;
}

export interface OidcSetup {
	publicBaseUrl: string;
	publicUrlConfigured: boolean;
	callbackUri: string;
	linkCallbackUri: string;
	postLogoutRedirectUri: string;
	httpsRequired: boolean;
}

export interface OidcDiscoveryResult {
	issuer: string;
	authorizationEndpoint: string;
	tokenEndpoint: string;
	userInfoEndpoint: string | null;
	endSessionEndpoint: string | null;
	issuerMatchesAuthority: boolean;
}

export interface ExternalLogin {
	id: number;
	provider: string;
	providerKey: string;
	providerDisplayName: string | null;
	linkedAt: Date;
	lastUsedAt: Date | null;
}

const optionalText = z
	.string()
	.trim()
	.transform((value) => (value === "" ? null : value));

export const OidcProviderSchema = z.object({
	displayName: z.string().trim().min(1, { message: "settings:sso.validation.display-name" }).max(200),
	authority: z
		.string()
		.trim()
		.url({ message: "settings:sso.validation.authority" })
		.refine((value) => value.startsWith("https://") || value.startsWith("http://"), {
			message: "settings:sso.validation.authority",
		}),
	clientId: z.string().trim().min(1, { message: "settings:sso.validation.client-id" }),
	clientSecret: z.string(),
	scopes: z.string().trim().min(1, { message: "settings:sso.validation.scopes" }),
	enabled: z.boolean(),
	autoProvisionUsers: z.boolean(),
	defaultRole: z.enum(OIDC_DEFAULT_ROLES),
	usernameClaimType: optionalText,
	emailClaimType: optionalText,
	displayNameClaimType: optionalText,
	pictureClaimType: optionalText,
	rolesClaimType: optionalText,
	adminGroupValue: optionalText,
});

export type OidcProviderForm = z.input<typeof OidcProviderSchema>;
export type OidcProviderRequest = z.output<typeof OidcProviderSchema>;
