<h3>Password Reset Request</h3>

<p>We have received a password reset request for your account. If you have initiated this request, please click the link below to reset your password.</p>

<div style="margin: 20px 0;">
    <a href="{{model.link}}" style="background-color: #007bff; color: white; padding: 12px 24px; text-decoration: none; border-radius: 4px; display: inline-block;">Reset Password</a>
</div>

<p>If the button above doesn't work, you can also copy and paste the following link into your browser:</p>
<p style="word-break: break-all;">{{model.link}}</p>

<p>If you did not request a password reset, please ignore this email. Your password will remain unchanged.</p>

<p>This link will expire in 24 hours for security reasons.</p>

<p>Best regards,<br>
The Aevatar Team</p>

------
<h3>{{L "PasswordReset"}}</h3>

<p>{{L "PasswordResetInfoInEmail"}}</p>

<div>
    <a href="{{model.link}}">{{L "ResetMyPassword"}}</a>
</div>
